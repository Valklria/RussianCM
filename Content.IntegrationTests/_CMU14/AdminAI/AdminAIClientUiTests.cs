using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Content.Client.Administration.UI.Bwoink;
using Content.Client.CMU14.AdminAI;
using Content.Shared.Administration.AdminAI;
using NUnit.Framework;
using Robust.Client;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Content.IntegrationTests.CMU14.AdminAI;

[TestFixture, NonParallelizable, Category("LocalIPC")]
public sealed class AdminAIClientUiTests
{
    [Test]
    public void WorkspaceIsInsideChatColumnAndHorizontalSplitKeepsTwoChildren()
    {
        var content = Environment.GetEnvironmentVariable("RUCMU_AI_CONTENT_RESOURCES")!;
        var root = Path.GetFullPath(Path.Combine(content, "..", ".."));
        var xml = XDocument.Load(Path.Combine(root, "Content.Client/Administration/UI/Bwoink/BwoinkControl.xaml"));
        var slot = xml.Descendants().Single(element => element.Attribute("Name")?.Value == "AIWorkspaceSlot");
        Assert.That(slot.Parent!.Name.LocalName, Is.EqualTo("BoxContainer"));
        Assert.That(slot.Parent.Elements().Any(element => element.Attribute("Name")?.Value == "BwoinkArea"), Is.True);
        var split = slot.Parent.Parent!;
        Assert.That(split.Name.LocalName, Is.EqualTo("SplitContainer"));
        Assert.That(split.Elements().Count(), Is.EqualTo(2));
    }

    private static RobustIntegrationTest.ClientIntegrationInstance StartClient()
    {
        var resources = Environment.GetEnvironmentVariable("RUCMU_AI_ENGINE_RESOURCES");
        var content = Environment.GetEnvironmentVariable("RUCMU_AI_CONTENT_RESOURCES");
        Assert.That(resources, Is.Not.Null.And.Not.Empty); Assert.That(content, Is.Not.Null.And.Not.Empty);
        return new(new RobustIntegrationTest.ClientIntegrationOptions
        {
            ContentStart = false, ContentAssemblies = [],
            Options = new GameControllerOptions
            {
                LoadConfigAndUserData = false, LoadContentResources = false,
                PrototypeDirectory = new ResPath("/AdminAIEmptyPrototypes"),
                MountOptions = new MountOptions(dirMounts: [resources!, content!], zipMounts: []),
            },
        });
    }
    private static void Culture(RobustIntegrationTest.ClientIntegrationInstance client, string culture)
    {
        var loc = client.ResolveDependency<ILocalizationManager>();
        var value = CultureInfo.GetCultureInfo(culture, predefinedOnly: false);
        loc.LoadCulture(value); loc.DefaultCulture = value;
    }
    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (var child in root.Children) { yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }

    [TestCase("ru-RU", 360)]
    [TestCase("ru-RU", 460)]
    [TestCase("en-US", 360)]
    public async Task PrivateWorkspaceFitsMinimumChatWidth(string culture, int width)
    {
        using var client = StartClient(); await client.WaitIdleAsync();
        await client.WaitAssertion(() =>
        {
            Culture(client, culture);
            var ui = client.ResolveDependency<IUserInterfaceManager>();
            var control = new AdminAIDraftControl(); ui.StateRoot.AddChild(control);
            try
            {
                AdminAIDraftRequest? opened = null; control.RequestDraft += request => opened = request;
                control.Select(new(Guid.NewGuid()));
                control.Receive(new(opened!.Channel, opened.SelectionId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                    AdminAIDraftStatus.Draft, "Проверьте источник перед отправкой.", [], "Normal/Ready", 0, 120));
                control.SetExpanded(true);
                var size = new Vector2(width, 260);
                foreach (var child in Descendants(control).Prepend(control)) { child.InvalidateMeasure(); child.InvalidateArrange(); }
                control.Measure(size); control.Arrange(new UIBox2(Vector2.Zero, size));
                foreach (var button in Descendants(control).OfType<Button>().Where(button => button.Name != "ai-expand"))
                {
                    Assert.That(button.Position.X, Is.GreaterThanOrEqualTo(0));
                    Assert.That(button.Position.X + button.Width, Is.LessThanOrEqualTo(width + 1), button.Name);
                }
            }
            finally { control.Orphan(); }
        });
    }

    [Test]
    public async Task InsertionEditsOnlyComposeFieldAndKeepsOperatorText()
    {
        using var client = StartClient(); await client.WaitIdleAsync();
        await client.WaitAssertion(() =>
        {
            Culture(client, "ru-RU");
            var control = new AdminAIDraftControl(); var requests = new List<AdminAIDraftRequest>();
            control.RequestDraft += requests.Add; control.Select(new(Guid.NewGuid()));
            var selection = requests.Single(); var sent = 0;
            var panel = new BwoinkPanel(_ => sent++);
            var input = Descendants(panel).OfType<LineEdit>().Single(); input.Text = "Ответ администратора.";
            control.InsertDraft += panel.InsertDraftText;
            control.Receive(new(selection.Channel, selection.SelectionId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                AdminAIDraftStatus.Draft, "Черновик\nИИ.", [], "Normal/Ready", 0, 120));
            control.InsertCurrentDraft();
            Assert.That(sent, Is.Zero, "Insertion must not submit ordinary ahelp");
            Assert.That(requests, Has.Count.EqualTo(1), "Insertion must not generate or send an RPC");
            Assert.That(input.Text, Is.EqualTo("Ответ администратора. Черновик ИИ."));
            control.Reset(); control.InsertCurrentDraft();
            Assert.That(sent, Is.Zero);
        });
    }

    [Test]
    public async Task PreviousSelectionCannotPopulateOrInsertNewPlayersWorkspace()
    {
        using var client = StartClient(); await client.WaitIdleAsync();
        await client.WaitAssertion(() =>
        {
            Culture(client, "ru-RU");
            var control = new AdminAIDraftControl(); var requests = new List<AdminAIDraftRequest>();
            control.RequestDraft += requests.Add; control.Select(new(Guid.NewGuid())); var old = requests.Last();
            control.Select(new(Guid.NewGuid()));
            var inserted = 0; control.InsertDraft += _ => inserted++;
            control.Receive(new(old.Channel, old.SelectionId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                AdminAIDraftStatus.Draft, "private old draft", [], "Normal/Ready", 0, 120));
            control.InsertCurrentDraft(); Assert.That(inserted, Is.Zero);
            Assert.That(Rope.Collapse(Descendants(control).OfType<TextEdit>().Single().TextRope), Is.Empty);
        });
    }

    [Test]
    public async Task ExpiredDraftCannotBeInserted()
    {
        using var client = StartClient(); await client.WaitIdleAsync();
        await client.WaitAssertion(() =>
        {
            Culture(client, "ru-RU");
            var control = new AdminAIDraftControl(); AdminAIDraftRequest? request = null;
            control.RequestDraft += value => request = value; control.Select(new(Guid.NewGuid()));
            var inserted = 0; control.InsertDraft += _ => inserted++;
            control.Receive(new(request!.Channel, request.SelectionId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                AdminAIDraftStatus.Draft, "expired", [], "Normal/Ready", 0, 0));
            control.InsertCurrentDraft(); Assert.That(inserted, Is.Zero);
        });
    }

    [Test]
    public async Task SharedAndClientAssembliesPassUnmodifiedEngineSandbox()
    {
        using var client = StartClient(); await client.WaitIdleAsync();
        await client.CheckSandboxed(typeof(AdminAIDraftState).Assembly);
        await client.CheckSandboxed(typeof(AdminAIDraftControl).Assembly);
    }
}
