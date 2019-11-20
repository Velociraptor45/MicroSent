using MicroSent.Models;
using System;
using System.Collections.Generic;

namespace MicroSent.ViewModels
{
    public class HomeViewModel
    {
        public string accountName { get; }
        public List<Rating> linkRatings { get; }
        public List<Rating> accountRatings { get; }

        public HomeViewModel(string accountName, List<Rating> linkRatings, List<Rating> accountRatings)
        {
            this.accountName = accountName;
            this.linkRatings = linkRatings;
            this.accountRatings = accountRatings;
        }
    }
}
