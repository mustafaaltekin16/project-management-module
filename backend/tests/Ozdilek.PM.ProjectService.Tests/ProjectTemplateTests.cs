using FluentAssertions;
using Ozdilek.PM.ProjectService.Domain;
using Ozdilek.PM.SharedKernel.Exceptions;
using Xunit;

namespace Ozdilek.PM.ProjectService.Tests;

public class ProjectTemplateTests
{
    [Fact]
    public void AddField_ThenRemoveField_RemovesIt()
    {
        var template = ProjectTemplate.Create("Şablon 14", ProjectType.MultiUnit);
        var field = template.AddField("Proje Yöneticisi", "Yönetici Seçiniz", "Açılır Menü", "Yönetici Listesi", isRequired: false);

        template.RemoveField(field.Id);

        template.Fields.Should().BeEmpty();
    }

    [Fact]
    public void RemoveField_WhenRequired_Throws()
    {
        var template = ProjectTemplate.Create("Şablon 14", ProjectType.MultiUnit);
        var field = template.AddField("Proje Adı", "Proje adını girin", "Normal Yazı", null, isRequired: true);

        var act = () => template.RemoveField(field.Id);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RemoveField_UnknownId_ThrowsNotFound()
    {
        var template = ProjectTemplate.Create("Şablon 14", ProjectType.MultiUnit);

        var act = () => template.RemoveField(Guid.NewGuid());

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void AddField_WithDuplicateLabel_Throws()
    {
        var template = ProjectTemplate.Create("Standart", ProjectType.Simple);
        template.AddField("İş Gerekçesi", "", "textarea", null, isRequired: true);

        var act = () => template.AddField("iş gerekçesi", "", "text", null, isRequired: false);

        act.Should().Throw<DomainException>().WithMessage("*aynı etikete*");
    }

    [Fact]
    public void AddField_ManualSelect_NormalizesOptions()
    {
        var template = ProjectTemplate.Create("Standart", ProjectType.Simple);

        var field = template.AddField(
            "Öncelik",
            "Öncelik seçin",
            "select",
            "manual",
            isRequired: true,
            options: [" Yüksek ", "Düşük", "yüksek", ""]);

        field.Kind.Should().Be(TemplateFieldKind.Custom);
        field.Options.Should().Equal("Yüksek", "Düşük");
    }

    [Fact]
    public void Update_ReplacesSchemaAndPreservesOrder()
    {
        var template = ProjectTemplate.Create("Eski Şablon", ProjectType.Simple);
        template.AddField("Eski Alan", "", "text", null, isRequired: false);

        template.Update(
            "Yeni Şablon",
            ProjectType.MultiUnit,
            [
                new TemplateFieldDefinition(
                    "Proje Adı", "", "text", null, true, true,
                    TemplateFieldKind.System, "projectName", null),
                new TemplateFieldDefinition(
                    "Risk Bilgileri", "Riskleri açıklayın", "section", null, false, true,
                    TemplateFieldKind.Section, null, null),
                new TemplateFieldDefinition(
                    "Risk Seviyesi", "", "select", "manual", true, true,
                    TemplateFieldKind.Custom, null, ["Düşük", "Yüksek"])
            ]);

        template.Name.Should().Be("Yeni Şablon");
        template.ApplicableProjectType.Should().Be(ProjectType.MultiUnit);
        template.Fields.Select(field => field.Label)
            .Should().Equal("Proje Adı", "Risk Bilgileri", "Risk Seviyesi");
        template.Fields.Select(field => field.SortOrder).Should().Equal(0, 1, 2);
    }
}
