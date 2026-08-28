using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace WebApi.OpenApi;

/// <summary>
/// Applies XML documentation comments (summary/remarks/returns/param) from the
/// generated <c>WebApi.xml</c> to each OpenAPI operation, so every endpoint
/// carries a human-readable summary, description and parameter help in Scalar.
/// </summary>
public sealed class XmlCommentsOperationTransformer : IOpenApiOperationTransformer
{
    private readonly Lazy<IReadOnlyDictionary<string, XElement>> _members;

    public XmlCommentsOperationTransformer()
    {
        _members = new Lazy<IReadOnlyDictionary<string, XElement>>(LoadMembers);
    }

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action)
            return Task.CompletedTask;

        var member = FindMember(action.MethodInfo);
        if (member is null)
            return Task.CompletedTask;

        var summary = Clean(member.Element("summary"));
        var remarks = Clean(member.Element("remarks"));
        var returns = Clean(member.Element("returns"));

        if (!string.IsNullOrWhiteSpace(summary))
            operation.Summary = summary;

        var description = string.Join(" ", new[] { remarks, returns }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(description))
            description = string.Empty;

        if (operation.Parameters is not null)
        {
            foreach (var param in member.Elements("param"))
            {
                var paramName = param.Attribute("name")?.Value;
                var text = param.Value.Trim();
                if (string.IsNullOrWhiteSpace(paramName) || string.IsNullOrWhiteSpace(text))
                    continue;

                var target = operation.Parameters.FirstOrDefault(p => p.Name == paramName);
                if (target is not null && string.IsNullOrWhiteSpace(target.Description))
                {
                    target.Description = text;
                }
                else if (target is null && paramName != "cancellationToken")
                {
                    // [FromQuery] model binding flattens the object into
                    // per-property parameters, so a <param name="query"> has no
                    // matching parameter — surface its text in the description.
                    description = description.Length == 0 ? text : $"{description} {text}";
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(description))
            operation.Description = description;

        return Task.CompletedTask;
    }

    private static IReadOnlyDictionary<string, XElement> LoadMembers()
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "WebApi.xml");
        if (!File.Exists(xmlPath))
            return new Dictionary<string, XElement>();

        var document = XDocument.Load(xmlPath);
        return document.Root?
            .Element("members")?
            .Elements("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal)
            ?? new Dictionary<string, XElement>();
    }

    private XElement? FindMember(MethodInfo method)
    {
        foreach (var candidate in CandidateMemberNames(method))
        {
            if (_members.Value.TryGetValue(candidate, out var member))
                return member;
        }

        return null;
    }

    private static IEnumerable<string> CandidateMemberNames(MethodInfo method)
    {
        var declaringType = method.DeclaringType;
        if (declaringType is null)
            yield break;

        var fullName = declaringType.FullName
            ?? $"{declaringType.Namespace}.{declaringType.Name}";

        var parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            yield return $"M:{fullName}.{method.Name}";
            yield break;
        }

        var parameterTypes = string.Join(",", parameters.Select(p => TypeName(p.ParameterType)));
        yield return $"M:{fullName}.{method.Name}({parameterTypes})";

        // Overloads that differ only by ref/out modifiers or return type are rare
        // in controllers; the exact signature above is what the compiler emits.
    }

    private static string TypeName(Type type)
    {
        var generic = type.IsGenericType
            ? $"{type.Namespace}.{type.Name.Split('`')[0]}{{{string.Join(",", type.GetGenericArguments().Select(TypeName))}}}"
            : type.FullName ?? type.Name;

        return generic;
    }

    private static string? Clean(XElement? element)
    {
        if (element is null)
            return null;

        var text = string.Join(" ", element.Nodes().OfType<XText>().Select(t => t.Value));
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }
}