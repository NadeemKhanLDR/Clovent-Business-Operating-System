using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.SmartPos;
using Clovent.Restaurant.Application.SmartCombos;
using Xunit;
namespace Clovent.Desktop.Tests.Restaurant.SmartPos;
public class SmartComboPresentationTests
{
    [Fact] public void PreviewAndGrid_UseNamesCurrencyAndEvidenceWithoutIds()
    {
        CurrencyDisplay.Configure("Rs.",2);
        var id=Guid.NewGuid(); var item=new ComboItem(id,"Biryani","Standard",100,null);
        var opportunity=new ComboOpportunity(id.ToString(),"Biryani Combo",[item],3,5,0.6m,0.75m,1.25m,"Biryani - Standard","Salad",100,95,null);
        var analysis=new ComboAnalysis(Guid.NewGuid(),DateTimeOffset.UtcNow.AddDays(-30),DateTimeOffset.UtcNow,5,0,0,[opportunity]);
        var text=SmartComboPreviewDialog.Evidence(opportunity,analysis);
        Assert.Contains("Biryani - Standard",text); Assert.Contains("Rs.100.00",text); Assert.Contains("3 of 5",text); Assert.Contains("Cost data unavailable",text); Assert.DoesNotContain(id.ToString(),text);
        var row=new SmartComboBuilderView.ComboRow(opportunity); Assert.Equal("95.00",row.SuggestedPrice); Assert.Equal("Cost unavailable",row.EstimatedMargin);
        Assert.DoesNotContain(typeof(SmartComboBuilderView.ComboRow).GetProperties(),p=>p.PropertyType==typeof(Guid) || p.Name=="Signature");
    }
}
