using RentoX.Application.Content;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Application.UnitTests;

public sealed class PagePlaceholdersTests
{
    private static readonly SiteInfoValues Full = new(
        "Əli Əliyev", "1234567890", "Bakı, Nizami küç. 1", "info@rentox.az", "+994 12 345 67 89", "1234", null, null, null, null);

    private static readonly SiteInfoValues Empty = new(null, null, null, null, null, null, null, null, null, null);

    [Fact]
    public void MarksAreReplacedWithTheOperatorsDetails()
    {
        string text = PagePlaceholders.Resolve("Operator: **{{operatorName}}**, VÖEN: {{voen}}, yazın: {{email}}.", Full);

        Assert.Equal("Operator: **Əli Əliyev**, VÖEN: 1234567890, yazın: info@rentox.az.", text);
    }

    [Fact]
    public void AMarkInsideASentenceBecomesADashWhenTheDetailIsEmpty()
    {
        string text = PagePlaceholders.Resolve("Yazın: {{email}} ünvanına, VÖEN: {{voen}}, bu ikisi birlikdə.", Empty);

        Assert.Equal("Yazın: — ünvanına, VÖEN: —, bu ikisi birlikdə.", text);
    }

    [Fact]
    public void ALabelLineWithAnEmptyDetailDisappearsSoNoEmptyLabelIsLeft()
    {
        const string source = "Ünvan: {{address}}\nE-poçt: {{email}}\nTelefon: {{phone}}\nQaynar xətt: {{hotline}}";
        SiteInfoValues onlyEmail = Empty with { Email = "info@rentox.az" };

        Assert.Equal("E-poçt: info@rentox.az", PagePlaceholders.Resolve(source, onlyEmail));
        Assert.Equal(string.Empty, PagePlaceholders.Resolve(source, Empty));
        Assert.Equal(
            "Ünvan: Bakı, Nizami küç. 1\nE-poçt: info@rentox.az\nTelefon: +994 12 345 67 89\nQaynar xətt: 1234",
            PagePlaceholders.Resolve(source, Full));
    }

    [Fact]
    public void ALineWithTwoMarksIsKeptWithDashes()
    {
        Assert.Equal(
            "Operator: —, VÖEN: —",
            PagePlaceholders.Resolve("Operator: {{operatorName}}, VÖEN: {{voen}}", Empty));
    }

    [Fact]
    public void TextWithoutMarksIsLeftAlone()
    {
        const string text = "## Başlıq\n\nSadə mətn {burada} heç nə dəyişmir.";

        Assert.Equal(text, PagePlaceholders.Resolve(text, Full));
    }

    [Fact]
    public void ATypoInAMarkIsCaughtBeforeItGoesLive()
    {
        PagePlaceholders.Validate("Düzgün: {{email}} və {{phone}}");

        DomainException error = Assert.Throws<DomainException>(() => PagePlaceholders.Validate("Səhv: {{emial}}"));
        Assert.Contains("emial", error.Message);
    }
}
