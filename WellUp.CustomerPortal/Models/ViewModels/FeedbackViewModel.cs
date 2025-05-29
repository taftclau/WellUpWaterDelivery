using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WellUp.Core.Database;

namespace WellUp.CustomerPortal.Models.ViewModels
{
    public class FeedbackViewModel
    {
        public List<Order> Orders { get; set; } = new List<Order>();
        public List<CustomerFeedback> ExistingFeedbacks { get; set; } = new List<CustomerFeedback>();
    }

    public class CreateFeedbackViewModel
    {
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Please select a feedback type")]
        [Display(Name = "Feedback Type")]
        public string FeedbackType { get; set; }

        [Required(ErrorMessage = "Please provide feedback details")]
        [Display(Name = "Your Feedback")]
        [MinLength(10, ErrorMessage = "Please provide at least 10 characters of feedback")]
        public string Description { get; set; }

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}