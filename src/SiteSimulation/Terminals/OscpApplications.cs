using System.Text.Json;

namespace SiteSimulation;

public enum OscpDevice { Workstation, Phone }
public sealed record OscpApplication(string Id, string Label, string Symbol, string View, string[] Profiles, string[] Aliases);
public sealed record OscpApplicationManifest(int Version, OscpApplication[] Applications);

/// <summary>Native application registration. Presentation bindings do not grant device or command authority.</summary>
public sealed class OscpApplications
{
    public IReadOnlyList<OscpApplication> All { get; }
    public OscpApplications(string json)
    {
        var manifest = JsonSerializer.Deserialize<OscpApplicationManifest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (manifest is null || manifest.Version != 1 || manifest.Applications is not { Length: > 0 and <= 32 })
            throw new ArgumentException("Invalid OSCP application manifest.");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var app in manifest.Applications)
        {
            if (app is null || string.IsNullOrWhiteSpace(app.Id) || !app.Id.Contains(':') || string.IsNullOrWhiteSpace(app.Label)
                || string.IsNullOrWhiteSpace(app.Symbol) || string.IsNullOrWhiteSpace(app.View)
                || app.Profiles is not { Length: > 0 } || app.Profiles.Any(p => p is not ("workstation" or "phone"))
                || app.Profiles.Distinct().Count() != app.Profiles.Length || app.Aliases is null
                || app.Aliases.Append(app.Id).Any(alias => string.IsNullOrWhiteSpace(alias) || !names.Add(alias)))
                throw new ArgumentException("Invalid or duplicate OSCP application registration.");
        }
        All = Array.AsReadOnly(manifest.Applications);
    }
    public OscpApplication[] For(OscpDevice device) => All.Where(app => app.Profiles.Contains(Profile(device))).ToArray();
    public OscpApplication? Find(string name, OscpDevice device) => For(device).SingleOrDefault(app => app.Id.Equals(name, StringComparison.OrdinalIgnoreCase)
        || app.Aliases.Contains(name, StringComparer.OrdinalIgnoreCase));
    public static string Profile(OscpDevice device) => device switch
    {
        OscpDevice.Workstation => "workstation", OscpDevice.Phone => "phone", _ => throw new ArgumentException("Unknown OSCP device profile.")
    };
}
