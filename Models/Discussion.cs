using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace QueueUnderflow.Models
{
    public class Discussion
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "The title is required")]
        public string Title { get; set; }

        [Required(ErrorMessage = "The content of the discussion is required")]
        public string Content { get; set; }

        public DateTime Date { get; set; }

        [Required(ErrorMessage = "The category is required")]
        public int? CategoryId { get; set; }

        public virtual Category? Category { get; set; }

        public virtual ICollection<Answer>? Answers { get; set; }

        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem>? Categ { get; set; }
    }
}
