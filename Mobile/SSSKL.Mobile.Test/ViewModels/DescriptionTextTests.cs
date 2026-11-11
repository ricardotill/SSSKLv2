using FluentAssertions;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Test.ViewModels;

[TestClass]
public class DescriptionTextTests
{
    [TestMethod]
    [DataRow(null, "")]
    [DataRow("", "")]
    [DataRow("<p>Hallo</p>", "Hallo")]
    [DataRow("<p>Regel 1</p><p>Regel 2</p>", "Regel 1\nRegel 2")]
    [DataRow("<p>Alinea 1</p><p><br></p><p>Alinea 2</p>", "Alinea 1\n\nAlinea 2")]
    [DataRow("<p>a<br>b</p>", "a\nb")]
    [DataRow("<p>Fish &amp; chips &lt;3&nbsp;!</p>", "Fish & chips <3 !")]
    [DataRow("<p><strong>Vet</strong> en <a href=\"https://x\">link</a></p>", "Vet en link")]
    [DataRow("<ul><li>een</li><li>twee</li></ul>", "een\ntwee")]
    public void ToPlainText_ConvertsTheEditorHtml(string? html, string expected) =>
        DescriptionText.ToPlainText(html).Should().Be(expected);

    [TestMethod]
    [DataRow(null, "")]
    [DataRow("   \n ", "")]
    [DataRow("Hallo", "<p>Hallo</p>")]
    [DataRow("Alinea 1\r\n\r\nAlinea 2\n", "<p>Alinea 1</p><p><br></p><p>Alinea 2</p>")]
    [DataRow("<script>x</script> & co", "<p>&lt;script&gt;x&lt;/script&gt; &amp; co</p>")]
    public void ToHtml_WritesQuillParagraphsAndEncodes(string? text, string expected) =>
        DescriptionText.ToHtml(text).Should().Be(expected);

    [TestMethod]
    public void ToHtml_RoundTripsThroughPlainText()
    {
        const string text = "Hoi!\n\nIk ben Jan & ik hou van <bier>.\nTweede regel";

        DescriptionText.ToPlainText(DescriptionText.ToHtml(text)).Should().Be(text);
    }

    [TestMethod]
    [DataRow("<p>Gewoon</p><p><br></p>", false)]
    [DataRow(null, false)]
    [DataRow("<p><strong>Vet</strong></p>", true)]
    [DataRow("<ol><li>een</li></ol>", true)]
    [DataRow("<p><a href=\"https://x\">x</a></p>", true)]
    [DataRow("<p class=\"ql-align-center\">x</p>", true)]
    public void HasRichFormatting_DetectsWhatPlainTextLoses(string? html, bool expected) =>
        DescriptionText.HasRichFormatting(html).Should().Be(expected);
}
