﻿using System;
using System.Collections.Generic;
using System.Linq;
using catalog.Data;
using catalog.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceStack.Redis;

namespace catalog.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly BookContext _context;
        private readonly IConfiguration _config;

        public IndexModel(ILogger<IndexModel> logger, IConfiguration config, BookContext context)
        {
            _logger = logger;
            _context = context;
            _config = config;
        }

        public void OnGet()
        {
            var books=new List<Book>();

            try  {
                books = _context.Books.ToList();
            }
            catch (Exception ex)  {
                ViewData["Error"]=ex.Message;
                ViewData["books"] = books;
                return;
            }
            
            //Get data about the shopping cart
            var client = GetRedisClient();
            if (client != null)
            {
                var cartItems = client.GetListCount("cart");
                ViewData["cartNo"] = cartItems;
            }

            ViewData["books"] = books;
            
        }

        public IActionResult OnPostAddToShoppingCart()
        {
            var client = GetRedisClient();
            if (client != null)
            {
                var bookId = int.Parse(Request.Form["bookId"]);

                if (!client.GetAllItemsFromList("cart").Contains(bookId.ToString()))
                {
                    var book = _context.Books.Find(bookId);
                    book.InStock--;
                    _context.SaveChanges();

                    client.AddItemToList("cart", bookId.ToString());
                }
            }

            return RedirectToPage();
        }

        public IActionResult OnPostLoad()
        {
            BookLoader.LoadBooks(_context);
            return RedirectToPage();
        }

        private IRedisClient GetRedisClient()
        {
            var conString = _config.GetValue<String>("Redis:ConnectionString");
            if (conString.IndexOf("PRIVATE_IP")>=0)  {
                return null;
            }
            var manager = new RedisManagerPool(conString);
            return manager.GetClient();
        }
    }
}
