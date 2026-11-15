using Sprechen.Forms.Pages;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Xunit;

namespace Sprechen.Forms.Tests.Pages;

public sealed class FormsStudyDirectEntryTests
{
    [Fact]
    public void Bind_ReturnsStudyApplicationAndFirstForm()
    {
        var bound = FormsStudyDirectEntry.Bind(CreateStudyCatalog());

        Assert.Equal("sample.checkout", bound.ApplicationId);
        Assert.Equal("address-demo", bound.FormId);
    }

    [Fact]
    public void Bind_OrdersFormsBySequenceThenFormId()
    {
        var catalog = new FormsCatalog
        {
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "study.umfrage",
                    DisplayName = new LocalizedText { ["de"] = "Umfrage" },
                    Description = new LocalizedText { ["de"] = "Studie" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    IsStudyApplication = true,
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("zweites"),
                            DisplayName = new LocalizedText { ["de"] = "Zweit" },
                            Description = new LocalizedText { ["de"] = "Form" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"],
                            Sequence = 2
                        },
                        new FormDescriptor
                        {
                            FormId = FormId.From("erstes"),
                            DisplayName = new LocalizedText { ["de"] = "Erst" },
                            Description = new LocalizedText { ["de"] = "Form" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"],
                            Sequence = 1
                        }
                    ]
                }
            ]
        };

        var bound = FormsStudyDirectEntry.Bind(catalog);

        Assert.Equal("study.umfrage", bound.ApplicationId);
        Assert.Equal("erstes", bound.FormId);
    }

    [Fact]
    public void Bind_WhenNoStudyApplication_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => FormsStudyDirectEntry.Bind(new FormsCatalog()));
    }

    [Fact]
    public void Bind_WhenTwoStudyApplications_Throws()
    {
        var catalog = new FormsCatalog
        {
            Applications =
            [
                CreateMarkedApplication("a", "form-a"),
                CreateMarkedApplication("b", "form-b")
            ]
        };

        Assert.Throws<InvalidOperationException>(() => FormsStudyDirectEntry.Bind(catalog));
    }

    [Fact]
    public void Bind_WhenStudyApplicationHasNoForm_Throws()
    {
        var catalog = new FormsCatalog
        {
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "study.umfrage",
                    DisplayName = new LocalizedText { ["de"] = "Umfrage" },
                    Description = new LocalizedText { ["de"] = "Studie" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    IsStudyApplication = true,
                    Forms = []
                }
            ]
        };

        Assert.Throws<InvalidOperationException>(() => FormsStudyDirectEntry.Bind(catalog));
    }

    private static FormsCatalog CreateStudyCatalog()
    {
        return new FormsCatalog
        {
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "sample.checkout",
                    DisplayName = new LocalizedText { ["de"] = "Checkout" },
                    Description = new LocalizedText { ["de"] = "Demo" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    IsStudyApplication = true,
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("address-demo"),
                            DisplayName = new LocalizedText { ["de"] = "Adresse" },
                            Description = new LocalizedText { ["de"] = "Demo" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"],
                            Sequence = 1
                        }
                    ]
                },
                new ApplicationDescriptor
                {
                    ApplicationId = "sample.other",
                    DisplayName = new LocalizedText { ["de"] = "Andere" },
                    Description = new LocalizedText { ["de"] = "Demo" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("other-form"),
                            DisplayName = new LocalizedText { ["de"] = "Andere" },
                            Description = new LocalizedText { ["de"] = "Demo" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                }
            ]
        };
    }

    private static ApplicationDescriptor CreateMarkedApplication(string applicationId, string formId)
    {
        return new ApplicationDescriptor
        {
            ApplicationId = applicationId,
            DisplayName = new LocalizedText { ["de"] = applicationId },
            Description = new LocalizedText { ["de"] = "Demo" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            IsStudyApplication = true,
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From(formId),
                    DisplayName = new LocalizedText { ["de"] = formId },
                    Description = new LocalizedText { ["de"] = "Demo" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                }
            ]
        };
    }
}
