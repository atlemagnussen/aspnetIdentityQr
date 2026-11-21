#nullable disable

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace AspAuth.Local
{
    [HtmlTargetElement("wa-checkbox", Attributes = "asp-for")]
    public class WaCheckboxTagHelper : TagHelper
    {
        [HtmlAttributeName("asp-for")]
        public ModelExpression For { get; set; }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // 1. Set the name and id attributes based on the model property
            output.Attributes.SetAttribute("name", For.Name);
            output.Attributes.SetAttribute("id", For.Name.Replace(".", "_"));

            // 2. Force the value to "true" (so it doesn't send "on")
            output.Attributes.SetAttribute("value", "true");

            // 3. Set the 'checked' attribute if the model value is true
            if (For.Model is bool boolValue && boolValue)
            {
                output.Attributes.SetAttribute("checked", null);
            }

            // 4. Generate the hidden input so 'false' is sent if unchecked
            // This mimics the standard ASP.NET Core checkbox behavior
            var hiddenInput = new TagBuilder("input");
            hiddenInput.Attributes.Add("type", "hidden");
            hiddenInput.Attributes.Add("name", For.Name);
            hiddenInput.Attributes.Add("value", "false");

            // Append the hidden input immediately after the custom element
            output.PostElement.AppendHtml(hiddenInput);
        }
    }
}