// WellUp.Core/Utilities/DefaultProductImages.cs
using System.Collections.Generic;

namespace WellUp.Core.Utilities
{
    public static class DefaultProductImages
    {
        public static readonly List<ProductImageOption> Options = new List<ProductImageOption>
        {
            new ProductImageOption
            {
                Id = "slim_container",
                Name = "Slim Container",
                Path = "/images/products/slim_container.png",
                Description = "Water Gallon - Slim Container"
            },
            new ProductImageOption
            {
                Id = "round_container",
                Name = "Round Container",
                Path = "/images/products/round_container.png",
                Description = "Water Gallon - Round Container"
            },
            new ProductImageOption
            {
                Id = "refill",
                Name = "Water Refill",
                Path = "/images/products/refill.png",
                Description = "Water Refill Service"
            }
        };

        public static ProductImageOption GetByContainerType(string containerType, bool includesRefill)
        {
            if (string.IsNullOrEmpty(containerType) && includesRefill)
                return Options.FirstOrDefault(o => o.Id == "refill");

            if (containerType?.ToLower() == "slim")
                return Options.FirstOrDefault(o => o.Id == "slim_container");

            if (containerType?.ToLower() == "round")
                return Options.FirstOrDefault(o => o.Id == "round_container");

            return Options.FirstOrDefault();
        }
    }

    public class ProductImageOption
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Description { get; set; }
    }
}