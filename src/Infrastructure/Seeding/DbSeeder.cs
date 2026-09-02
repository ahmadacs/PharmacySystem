using Application.Common.Security;
using Domain.Entities.Medicines;
using Domain.Entities.Patients;
using Domain.Entities.Prescriptions;
using Domain.Entities.Staff;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Seeding;
/// <summary>
/// Idempotent seed data for local development. Runs automatically on first
/// startup after the database is created/migrated.
/// </summary>
public static class DbSeeder
{
    public const string AdminEmail = "admin@pharmacy.com";
    public const string PharmacistEmail = "pharmacist@pharmacy.com";
    public const string DoctorEmail = "doctor@pharmacy.com";
    private const string AdminPassword = "Admin@1234";
    private const string PharmacistPassword = "Pharma@1234";
    private const string DoctorPassword = "Doctor@1234";
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        // Ensure roles exist
        foreach (var role in new[] { Roles.Admin, Roles.Pharmacist, Roles.Doctor })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole(role));
        }

        // Ensure users exist
        var admin = await EnsureUserAsync(userManager, AdminEmail, AdminPassword, "Pharmacy", "Administrator", Roles.Admin);
        var pharmacist = await EnsureUserAsync(userManager, PharmacistEmail, PharmacistPassword, "Rania", "Khalil", Roles.Pharmacist);
        var doctor = await EnsureUserAsync(userManager, DoctorEmail, DoctorPassword, "Omar", "Haddad", Roles.Doctor);

        await db.SaveChangesAsync(cancellationToken);

        if (!await db.Medicines.AnyAsync(cancellationToken))
            await SeedMedicinesAsync(db, cancellationToken);

        if (!await db.Prescriptions.AnyAsync(cancellationToken))
            await SeedPrescriptionsAsync(db, doctor, pharmacist, cancellationToken);
    }

    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string firstName, string lastName, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName
            };
            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, role);
        }
        return user;
    }

    private static async Task SeedMedicinesAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var genericNames = new List<GenericName>
        {
            new("Acetaminophen", "باراسيتامول"),
            new("Ibuprofen", "إيبوبروفين"),
            new("Amoxicillin", "أموكسيسيلين"),
            new("Cetirizine", "سيتيريزين"),
            new("Metformin Hydrochloride", "ميتفورمين"),
            new("Loratadine", "لوراتادين"),
        };

        await db.GenericNames.AddRangeAsync(genericNames, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        // Each medicine carries its own variants (some medicines have several
        // forms, e.g. Paracetamol -> Tablet + Syrup + Drops).
        var medicines = new List<(Medicine Medicine, (MedicineForm Form, MedicineUnit Unit, decimal Strength)[] Variants)>
        {
            (new Medicine("Paracetamol", CategoryEnum.Analgesics, genericNames.First(g => g.Name == "Acetaminophen"), false, "باراسيتامول"),
                new[] { (MedicineForm.Tablet, MedicineUnit.Mg, 500m), (MedicineForm.Syrup, MedicineUnit.Ml, 120m), (MedicineForm.Drops, MedicineUnit.Ml, 100m) }),
            (new Medicine("Ibuprofen", CategoryEnum.Analgesics, genericNames.First(g => g.Name == "Ibuprofen"), false, "إيبوبروفين"),
                new[] { (MedicineForm.Capsule, MedicineUnit.Mg, 200m), (MedicineForm.Tablet, MedicineUnit.Mg, 400m), (MedicineForm.Suspension, MedicineUnit.Ml, 100m) }),
            (new Medicine("Amoxicillin", CategoryEnum.Antibiotics, genericNames.First(g => g.Name == "Amoxicillin"), true, "أموكسيسيلين"),
                new[] { (MedicineForm.Capsule, MedicineUnit.Mg, 250m), (MedicineForm.Suspension, MedicineUnit.Ml, 250m) }),
            (new Medicine("Cetirizine", CategoryEnum.Antihistamines, genericNames.First(g => g.Name == "Cetirizine"), false, "سيتيريزين"),
                new[] { (MedicineForm.Tablet, MedicineUnit.Mg, 10m), (MedicineForm.Syrup, MedicineUnit.Ml, 5m) }),
            (new Medicine("Metformin", CategoryEnum.Antidiabetics, genericNames.First(g => g.Name == "Metformin Hydrochloride"), false, "ميتفورمين"),
                new[] { (MedicineForm.Tablet, MedicineUnit.Mg, 500m), (MedicineForm.Tablet, MedicineUnit.Mg, 850m) }),
            (new Medicine("Loratadine", CategoryEnum.Antihistamines, genericNames.First(g => g.Name == "Loratadine"), false, "لوراتادين"),
                new[] { (MedicineForm.Tablet, MedicineUnit.Mg, 10m), (MedicineForm.Syrup, MedicineUnit.Ml, 5m) }),
        };

        foreach (var entry in medicines)
        {
            db.Medicines.Add(entry.Medicine);
        }

        await db.SaveChangesAsync(cancellationToken);

        // Add variants and batches for each medicine. Batch numbers use a single
        // global counter so they are unique across the whole system (matches the
        // AddBatch rule that forbids duplicate batch numbers). Expiry dates rotate
        // through Safe / Critical / Warning offsets so the expiry-alerts tab and
        // its badge show a meaningful mix on a fresh database. Batch quantities are
        // entered in whole packages and converted to base units via the variant's
        // UnitOfMeasure (e.g. 4 boxes of 30 tablets = 120 tablets). Amoxicillin and
        // Cetirizine are seeded with a single package each so the low-stock list
        // (and badge) have entries too.
        int batchCounter = 0;
        int expiryIndex = 0;
        int[] expiryOffsets = { 320, 320, 15, 60, 320, 320, 15, 60 };
        foreach (var (medicine, variants) in medicines)
        {
            foreach (var (form, unit, strength) in variants)
            {
                var uom = UnitOfMeasureFor(form);
                // Each variant now carries its own reorder level (10 by default, matches the DTO default).
                var variant = new MedicineVariant(medicine.Id, form, unit, strength, 10, uom);
                medicine.AddVariant(variant);
                db.MedicineVariants.Add(variant);

                var targetQuantity = medicine.Name switch
                {
                    "Amoxicillin" => 5,
                    "Cetirizine" => 0,
                    _ => 100
                };

                var packages = Math.Max(1, (int)Math.Ceiling(targetQuantity / (double)uom.UnitsPerPackage));
                var batchCount = medicine.Name is "Amoxicillin" or "Cetirizine" ? 1 : 2;

                for (int b = 0; b < batchCount; b++)
                {
                    var mb = new MedicineBatch(variant.Id, $"B{++batchCounter:D3}", Manufactured(400), Expiry(expiryOffsets[expiryIndex++ % expiryOffsets.Length]), packages, uom, 1.25m);
                    db.MedicineBatches.Add(mb);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static UnitOfMeasure UnitOfMeasureFor(MedicineForm form) => form switch
    {
        MedicineForm.Tablet or MedicineForm.Chewable or MedicineForm.Lozenges
            or MedicineForm.Effervescent or MedicineForm.Granules => UnitOfMeasure.Create("Tablet", "Box", 30, isDivisible: true),
        MedicineForm.Capsule => UnitOfMeasure.Create("Capsule", "Box", 30, isDivisible: true),
        _ => UnitOfMeasure.Create("Bottle", "Carton", 6, isDivisible: true)
    };

    private static DateOnly Manufactured(int daysAgo) => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-daysAgo));

    private static DateOnly Expiry(int daysFromNow) => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysFromNow));

    private static async Task SeedPrescriptionsAsync(ApplicationDbContext db, ApplicationUser doctor, ApplicationUser pharmacist, CancellationToken cancellationToken)
    {
        var medicines = await db.Medicines
            .Include(m => m.Variants)
            .ThenInclude(v => v.Batches)
            .ToListAsync(cancellationToken);

        if (medicines.Count == 0)
            return;

        var doctorProfile = await db.Doctors.FirstOrDefaultAsync(d => d.UserId == doctor.Id, cancellationToken);
        if (doctorProfile is null)
        {
            doctorProfile = new Doctor(doctor.Id, "D20260001", "General Medicine", "0550000000");
            db.Doctors.Add(doctorProfile);
        }

        if (!await db.Pharmacists.AnyAsync(p => p.UserId == pharmacist.Id, cancellationToken))
            db.Pharmacists.Add(new Pharmacist(pharmacist.Id, "P20260001"));

        var john = await GetOrCreatePatientAsync(db, "John", "Smith", new DateOnly(1990, 5, 15), "+966500000001", cancellationToken);
        var jane = await GetOrCreatePatientAsync(db, "Jane", "Doe", new DateOnly(1985, 11, 2), "+966500000002", cancellationToken);

        var variants = medicines.SelectMany(m => m.Variants).ToList();
        if (variants.Count == 0)
            return;

        var prescriptions = new List<Prescription>
        {
            new(doctorProfile.Id, john.Id, DateOnly.FromDateTime(DateTime.UtcNow), "Headache", false, 3),
            new(doctorProfile.Id, jane.Id, DateOnly.FromDateTime(DateTime.UtcNow), "Cold symptoms", false, 2),
        };

        prescriptions[0].AddItem(variants[0].Id, 10, "One tablet every 6 hours");
        prescriptions[1].AddItem(variants.Count > 1 ? variants[1].Id : variants[0].Id, 5, "One tablet at bedtime");

        foreach (var prescription in prescriptions)
        {
            db.Prescriptions.Add(prescription);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Patient> GetOrCreatePatientAsync(ApplicationDbContext db, string firstName, string lastName, DateOnly dateOfBirth, string phoneNumber, CancellationToken cancellationToken)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(
            p => p.PhoneNumber == phoneNumber, cancellationToken);
        if (patient is null)
        {
            patient = new Patient(firstName, lastName, dateOfBirth, phoneNumber);
            db.Patients.Add(patient);
        }

        return patient;
    }
}