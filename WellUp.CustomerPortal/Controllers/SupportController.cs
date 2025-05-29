//Controllers/SupportController.cs
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using WellUp.CustomerPortal.Models.ViewModels;

namespace WellUp.CustomerPortal.Controllers
{
    public class SupportController : Controller
    {
        public IActionResult Index()
        {
            var viewModel = new SupportViewModel
            {
                PageTitle = "Help & Support",
                PageDescription = "We're here to help you with any questions or issues you may have."
            };

            return View(viewModel);
        }

        public IActionResult Faq()
        {
            var viewModel = new FaqViewModel
            {
                PageTitle = "Frequently Asked Questions",
                PageDescription = "Find answers to the most common questions about WellUp's water delivery service.",
                Categories = new List<FaqCategory>
                {
                    new FaqCategory
                    {
                        Name = "Ordering",
                        Icon = "shopping-cart",
                        Items = new List<FaqItem>
                        {
                            new FaqItem
                            {
                                Question = "How do I place an order?",
                                Answer = "You can place an order by logging into your account, browsing our products, and adding items to your cart. Once you've added all desired items, proceed to checkout and follow the prompts to complete your order."
                            },
                            new FaqItem
                            {
                                Question = "Can I modify or cancel my order after it's placed?",
                                Answer = "Yes, you can cancel your order as long as it still has the 'New' status. Simply go to your Order History, find the order, and click the Cancel button. Orders that are already in progress cannot be cancelled."
                            },
                            new FaqItem
                            {
                                Question = "Is there a minimum order requirement?",
                                Answer = "No, there is no minimum order requirement. You can order as little as one container or refill at a time."
                            },
                            new FaqItem
                            {
                                Question = "How do I reorder previous purchases?",
                                Answer = "You can easily reorder previous purchases by going to your Order History, finding the order you want to repeat, and clicking the 'Reorder' button. This will add all available items from that order to your cart."
                            }
                        }
                    },
                    new FaqCategory
                    {
                        Name = "Delivery",
                        Icon = "truck",
                        Items = new List<FaqItem>
                        {
                            new FaqItem
                            {
                                Question = "What are your delivery hours?",
                                Answer = "Our delivery hours are from 8:00 AM to 6:30 PM, seven days a week. You can select your preferred delivery time during checkout."
                            },
                            new FaqItem
                            {
                                Question = "Is same-day delivery available?",
                                Answer = "Yes, same-day delivery is available for orders placed before 5:00 PM. Orders placed after this time will be scheduled for the next day or a future date of your choice."
                            },
                            new FaqItem
                            {
                                Question = "How do I track my delivery?",
                                Answer = "You can track your delivery by checking your Order History in your account. The current status of your order and estimated delivery time will be displayed there."
                            },
                            new FaqItem
                            {
                                Question = "What if I'm not home during delivery?",
                                Answer = "We recommend selecting a delivery time when someone will be available to receive your order. If you're unexpectedly unavailable, please contact our customer service to reschedule your delivery."
                            },
                            new FaqItem
                            {
                                Question = "Do you deliver to my area?",
                                Answer = "We currently deliver to all areas within Angeles City, Pampanga. If you're unsure if your location is within our delivery range, please contact our customer service."
                            }
                        }
                    },
                    new FaqCategory
                    {
                        Name = "Payment",
                        Icon = "money-bill-wave",
                        Items = new List<FaqItem>
                        {
                            new FaqItem
                            {
                                Question = "What payment methods do you accept?",
                                Answer = "Currently, we only accept Cash on Delivery (COD) as our payment method. Please have the exact amount ready upon delivery."
                            },
                            new FaqItem
                            {
                                Question = "Is there a delivery fee?",
                                Answer = "No, we do not charge any additional delivery fees. The price you see for the products includes delivery service."
                            },
                            new FaqItem
                            {
                                Question = "Can I tip the delivery personnel?",
                                Answer = "Tipping is not required but is always appreciated by our delivery personnel if you're satisfied with their service."
                            }
                        }
                    },
                    new FaqCategory
                    {
                        Name = "Products & Services",
                        Icon = "water",
                        Items = new List<FaqItem>
                        {
                            new FaqItem
                            {
                                Question = "What types of water containers do you offer?",
                                Answer = "We offer two types of containers: round (standard) and slim (space-saving design). Both are available as container-only or with water refill included."
                            },
                            new FaqItem
                            {
                                Question = "Can I order water refill for my own container?",
                                Answer = "Yes, you can order water refill for your own container. When placing your order, select the 'Water Refill' option and provide details about your container in the special instructions field."
                            },
                            new FaqItem
                            {
                                Question = "How long does the water stay fresh?",
                                Answer = "Our purified water stays fresh for up to 2 months when stored properly in a cool, dry place away from direct sunlight. Once opened, we recommend consuming it within 2 weeks for the best quality."
                            },
                            new FaqItem
                            {
                                Question = "How is your water purified?",
                                Answer = "Our water undergoes a rigorous purification process including multi-stage filtration, reverse osmosis, UV treatment, and ozonation to ensure it's clean, safe, and great-tasting."
                            }
                        }
                    },
                    new FaqCategory
                    {
                        Name = "Account & Settings",
                        Icon = "user-cog",
                        Items = new List<FaqItem>
                        {
                            new FaqItem
                            {
                                Question = "How do I update my personal information?",
                                Answer = "You can update your personal information by going to Account Settings and selecting Edit Profile. There you can modify your name and phone number."
                            },
                            new FaqItem
                            {
                                Question = "How do I add or change my delivery address?",
                                Answer = "You can manage your delivery addresses in Account Settings under the Manage Addresses section. You can add new addresses, edit existing ones, or set a default address for your deliveries."
                            },
                            new FaqItem
                            {
                                Question = "I forgot my password. How do I reset it?",
                                Answer = "On the login page, click the 'Forgot Password' link and follow the instructions. You'll need to verify your identity using your registered email, phone number, and ZIP code."
                            }
                        }
                    }
                }
            };

            return View(viewModel);
        }
    }
}