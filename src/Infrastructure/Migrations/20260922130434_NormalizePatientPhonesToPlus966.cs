using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizePatientPhonesToPlus966 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Canonical form is +9665XXXXXXXX: unify 00966.../05.../5... spellings
            // so one person can never exist as two patient rows again.
            migrationBuilder.Sql(@"
                UPDATE Patients
                SET PhoneNumber = '+966' + SUBSTRING(PhoneNumber, 6, LEN(PhoneNumber) - 5)
                WHERE PhoneNumber LIKE '00966%';
            ");
            migrationBuilder.Sql(@"
                UPDATE Patients
                SET PhoneNumber = '+966' + SUBSTRING(PhoneNumber, 2, LEN(PhoneNumber) - 1)
                WHERE LEN(PhoneNumber) = 10 AND PhoneNumber LIKE '05%';
            ");
            migrationBuilder.Sql(@"
                UPDATE Patients
                SET PhoneNumber = '+966' + PhoneNumber
                WHERE LEN(PhoneNumber) = 9 AND PhoneNumber LIKE '5%' AND PhoneNumber NOT LIKE '+%';
            ");

            // Merge rows that became duplicates: move their prescriptions to the
            // earliest-created survivor per canonical phone (one phone = one
            // patient is the invariant the API already enforces with 409).
            migrationBuilder.Sql(@"
                UPDATE pr
                SET pr.PatientId = surv.SurvivorId
                FROM Prescriptions pr
                INNER JOIN (
                    SELECT p.Id AS DuplicateId,
                        (SELECT TOP (1) p2.Id FROM Patients p2
                         WHERE p2.PhoneNumber = p.PhoneNumber
                         ORDER BY p2.CreatedAt ASC, p2.Id ASC) AS SurvivorId
                    FROM Patients p
                ) AS surv ON surv.DuplicateId = pr.PatientId
                WHERE surv.DuplicateId <> surv.SurvivorId;
            ");
            migrationBuilder.Sql(@"
                DELETE p
                FROM Patients p
                WHERE EXISTS (
                    SELECT 1 FROM Patients s
                    WHERE s.PhoneNumber = p.PhoneNumber
                      AND (s.CreatedAt < p.CreatedAt
                           OR (s.CreatedAt = p.CreatedAt AND s.Id < p.Id))
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-only migration: the original per-row spelling (05... vs +966...)
            // is unrecoverable after canonicalization and merging, so Down is
            // intentionally a no-op. Code before this migration reads canonical
            // numbers fine (exact match), it just cannot dedupe new ones.
        }
    }
}
