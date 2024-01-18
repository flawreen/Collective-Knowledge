using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;

namespace QueueUnderflow.Models
{
    public class ApplicationUser : IdentityUser
    {
        public virtual ICollection<Answer>? Answers { get; set; }

        public virtual ICollection<Discussion>? Discussions { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem>? AllRoles { get; set; }
    }
}
