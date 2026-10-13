using Content.Server.CMU14.AdminAI;
using NUnit.Framework;
using Robust.Shared.Configuration;

namespace Content.IntegrationTests.CMU14.AdminAI;

[TestFixture]
public sealed class AdminAIConfigurationTests
{
    [Test]
    public void ConfigurationIsServerOnlyAndDisabledByDefault()
    {
        Assert.That(AdminAICVars.Enabled.DefaultValue, Is.False);
        foreach (var definition in new CVarDef[]
        {
            AdminAICVars.Enabled, AdminAICVars.SocketPath, AdminAICVars.TimeoutSeconds,
            AdminAICVars.MaxInputChars, AdminAICVars.MaxOutputTokens, AdminAICVars.MaxOutputChars,
        })
        {
            Assert.That(definition.Flags.HasFlag(CVar.SERVERONLY), Is.True);
            Assert.That((definition.Flags & (CVar.REPLICATED | CVar.NOTIFY)), Is.EqualTo(CVar.NONE));
        }
    }
}
