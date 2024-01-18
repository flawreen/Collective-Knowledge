using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueUnderflow.Data;
using QueueUnderflow.Models;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;

namespace QueueUnderflow.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CategoriesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }


        public IActionResult Index()
        {
            if (TempData["notification"] != null)
            {
                ViewBag.Notification = TempData["notification"];
                ViewBag.Icon = TempData["icon"];
                ViewBag.Type = TempData["type"];
            }

            var categories = db.Categories.Include("Discussions");
            ViewBag.Categories = categories;

            return View();
        }

        public ActionResult Show(int id)
        {
            Category categ = db.Categories.Include("Discussions").Where(cat => cat.Id == id).First();

            int _perPage = 3;

            IOrderedQueryable<Discussion>? discussions;
            var sortType = Convert.ToString(HttpContext.Request.Query["sort"]);
            ViewBag.SortType = sortType;
            if (sortType == "answers")
            {
                discussions = db.Discussions.Include("Category").Include("User")
                .Where(disc => disc.CategoryId == id).OrderByDescending(a => a.Answers.Count() );
            }
            else
            {
                discussions = db.Discussions.Include("Category").Include("User")
                .Where(disc => disc.CategoryId == id).OrderByDescending(a => a.Date);
            }

            int totalItems = discussions.Count();

            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);
            
            var offset = 0;
            
            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * _perPage;
            }
            var paginatedDiscussions = discussions.Skip(offset).Take(_perPage);

            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);
            ViewBag.Discussions = paginatedDiscussions;
            return View(categ);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult New()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult New(Category categ)
        {
            if (User.IsInRole("Admin"))
            {
                if (ModelState.IsValid)
                {
                    db.Categories.Add(categ);
                    db.SaveChanges();
                    TempData["notification"] = "Category added";
                    TempData["icon"] = "bi-plus-circle";
                    TempData["type"] = "bg-success";
                    return RedirectToAction("Index");
                }
                else
                {
                    return View();
                }
            }
            else
            {
                TempData["notification"] = "Not allowed";
                TempData["icon"] = "bi-exclamation-triangle";
                TempData["type"] = "bg-danger";
                return RedirectToAction("Index");
            }
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            Category categ = db.Categories.Find(id);

            return View( categ );
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult Edit(int id, Category requestCateg)
        {
            Category categ = db.Categories.Find(id);

            if (User.IsInRole("Admin"))
            {
                if (ModelState.IsValid)
                {
                    categ.CategoryName = requestCateg.CategoryName;
                    db.SaveChanges();
                    TempData["notification"] = "Category edited.";
                    TempData["icon"] = "bi-pencil";
                    TempData["type"] = "bg-info";
                    return RedirectToAction("Index");
                }
                else
                {
                    return View(requestCateg);
                }
            }
            else
            {
                TempData["notification"] = "Not allowed";
                TempData["icon"] = "bi-exclamation-triangle";
                TempData["type"] = "bg-danger";
                return RedirectToAction("Index");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult Delete(int id)
        {
            if (User.IsInRole("Admin"))
            {
                var categ = db.Categories
                .Include("Discussions")
                .Include("Discussions.Answers")
                .Where(c => c.Id == id)
                .First();

                if (categ.Discussions.Count > 0)
                {
                    foreach (var discussion in categ.Discussions)
                    {
                        if (discussion.Answers.Count() > 0)
                        {
                            foreach (var ans in discussion.Answers)
                            {
                                db.Answers.Remove(ans);
                            }
                        }
                        db.Discussions.Remove(discussion);
                    }
                }

                db.Categories.Remove(categ);
                db.SaveChanges();

                TempData["notification"] = "Category deleted.";
                TempData["icon"] = "bi-trash3";
                TempData["type"] = "bg-danger";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["notification"] = "Not allowed";
                TempData["icon"] = "bi-exclamation-triangle";
                TempData["type"] = "bg-danger";
                return RedirectToAction("Index");
            }
        }
    }
}
