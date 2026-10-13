using Robust.Shared.Configuration;

namespace Content.Server.CMU14.AdminAI;

/// <summary>Stage A configuration. Only a manual health probe consumes these; ahelp is not connected.</summary>
[CVarDefs]
public static class AdminAICVars
{
    public static readonly CVarDef<bool> Enabled =
        CVarDef.Create("cmu.admin_ai.enabled", false, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> SocketPath =
        CVarDef.Create("cmu.admin_ai.socket_path", "/run/rucmu-ai/llama.sock", CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> TimeoutSeconds =
        CVarDef.Create("cmu.admin_ai.request_timeout_seconds", 45, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> MaxInputChars =
        CVarDef.Create("cmu.admin_ai.max_input_chars", 4096, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> MaxOutputTokens =
        CVarDef.Create("cmu.admin_ai.max_output_tokens", 256, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> MaxOutputChars =
        CVarDef.Create("cmu.admin_ai.max_output_chars", 4096, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>Reads a fresh snapshot; invalid values always disable it.</summary>
    public static AIInferenceOptions Read(IConfigurationManager config)
    {
        var options = new AIInferenceOptions(
            config.GetCVar(Enabled), config.GetCVar(SocketPath), config.GetCVar(TimeoutSeconds),
            config.GetCVar(MaxInputChars), config.GetCVar(MaxOutputTokens), config.GetCVar(MaxOutputChars));
        return options.IsValid() ? options : options with { Enabled = false };
    }
}
