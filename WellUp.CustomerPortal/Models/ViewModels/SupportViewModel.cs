// WellUp.CustomerPortal.Models.ViewModels/SupportViewModel.cs

using System.Collections.Generic;

namespace WellUp.CustomerPortal.Models.ViewModels
{
    public class SupportViewModel
    {
        public string PageTitle { get; set; }
        public string PageDescription { get; set; }
    }

    public class FaqViewModel
    {
        public string PageTitle { get; set; }
        public string PageDescription { get; set; }
        public List<FaqCategory> Categories { get; set; } = new List<FaqCategory>();
    }

    public class FaqCategory
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public List<FaqItem> Items { get; set; } = new List<FaqItem>();
    }

    public class FaqItem
    {
        public string Question { get; set; }
        public string Answer { get; set; }
    }
}