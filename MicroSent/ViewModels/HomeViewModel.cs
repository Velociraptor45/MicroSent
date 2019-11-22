using MicroSent.Models;
using System.Collections.Generic;

namespace MicroSent.ViewModels
{
    public class HomeViewModel
    {
        public string accountName { get; set; }
        public List<Rating> linkRatings { get; set; }
        public List<Rating> accountRatings { get; set; }

        public HomeViewModel()
        {

        }

        public HomeViewModel(string accountName, List<Rating> linkRatings, List<Rating> accountRatings)
        {
            this.accountName = accountName;
            this.linkRatings = linkRatings;
            this.accountRatings = accountRatings;
        }
    }
}
