using Sprechen.Speech.State;

namespace Sprechen.Speech.State.Tests.DialogHostExamples;

public sealed class DialogHostWizardExampleTests
{
    [Fact]
    public async Task DialogHost_LanguageSelection_ThenSearchLoadApp_ThenFillFieldsAndNavigate()
    {
        var context = new WizardContext(new[]
        {
            new WizardLanguage("de", "Deutsch"),
            new WizardLanguage("en", "English")
        });

        var catalog = WizardFormsCatalog.CreateSample();

        await using var host = new DialogHost<WizardEvent>();
        var root = new LanguageSelectionFlow(host, context, catalog);

        await host.StartAsync(root);

        Assert.Equal(1, host.Depth);

        await host.DispatchToActiveAsync(new WizardLanguageSelectedEvent("de"));

        Assert.Equal("de", context.SelectedLanguageCode);
        Assert.True(root.IsDisposed);
        Assert.Equal(1, host.Depth);

        await host.DispatchToActiveAsync(new WizardApplicationSearchRequestedEvent("Kurz"));
        await host.DispatchToActiveAsync(new WizardApplicationSelectedEvent("short-form"));

        Assert.Equal("short-form", context.SelectedApplicationId);
        Assert.Equal(WizardFieldId.LastName, context.FocusField);

        await host.DispatchToActiveAsync(new WizardUserTextEvent("Müller"));
        await host.DispatchToActiveAsync(new WizardNavigateNextEvent());

        Assert.Equal(WizardFieldId.Email, context.FocusField);

        await host.DispatchToActiveAsync(new WizardUserTextEvent("name@beispiel.de"));
        await host.DispatchToActiveAsync(new WizardNavigateNextEvent());

        Assert.Null(context.FocusField);
        Assert.Equal("Müller", context.FieldValues[WizardFieldId.LastName]);
        Assert.Equal("name@beispiel.de", context.FieldValues[WizardFieldId.Email]);
        Assert.Equal("de", context.SelectedLanguageCode);

        Assert.Contains(context.Prompts, prompt => prompt.Contains("Sprache gesetzt", StringComparison.Ordinal));
        Assert.Contains(context.Prompts, prompt => prompt.Contains("Gefunden", StringComparison.Ordinal));
        Assert.Contains(context.Prompts, prompt => prompt.Contains("Kurzformular", StringComparison.Ordinal));
        Assert.Contains(context.Prompts, prompt => prompt.Contains("Nachname", StringComparison.Ordinal));
        Assert.Contains(context.Prompts, prompt => prompt.Contains("E-Mail", StringComparison.Ordinal));

        // Die gewählte Sprache soll auch beim Wechsel zu einer anderen Anwendung erhalten bleiben.
        await host.ReplaceTopAsync(new ApplicationSearchFlow(host, context, catalog));

        Assert.Equal("de", context.SelectedLanguageCode);

        await host.DispatchToActiveAsync(new WizardApplicationSearchRequestedEvent("Adress"));
        await host.DispatchToActiveAsync(new WizardApplicationSelectedEvent("address-form"));

        Assert.Equal("address-form", context.SelectedApplicationId);
        Assert.Equal(WizardFieldId.Street, context.FocusField);

        await host.DispatchToActiveAsync(new WizardUserTextEvent("Goethestraße 12"));
        await host.DispatchToActiveAsync(new WizardNavigateNextEvent());

        Assert.Null(context.FocusField);
        Assert.Equal("de", context.SelectedLanguageCode);
        Assert.Equal("Goethestraße 12", context.FieldValues[WizardFieldId.Street]);

        Assert.Contains(context.Prompts, prompt => prompt.Contains("Adressformular", StringComparison.Ordinal));
        Assert.Contains(context.Prompts, prompt => prompt.Contains("Straße", StringComparison.Ordinal));
    }
}


