﻿
using System.ComponentModel.DataAnnotations;

namespace Webshop.Api.Models.Dto.Product
{
    public class ProductQueryDto
    {
        public string Filter { get; set; }

        [Required]
        public SortCriteria SortCriteria { get; set; }
    }
}
