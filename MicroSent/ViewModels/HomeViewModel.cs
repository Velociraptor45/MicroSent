using System;
using System.Collections.Generic;

namespace MicroSent.ViewModels
{
    public class HomeViewModel
    {
        public string accountName { get; }
        public List<Tuple<string, int>> linkRatings { get; }
        public List<Tuple<string, int>> accountRatings { get; }

        public HomeViewModel(string accountName, List<Tuple<string, int>> linkRatings,
            List<Tuple<string, int>> accountRatings)
        {
            this.accountName = accountName;
            this.linkRatings = linkRatings;
            this.accountRatings = accountRatings;
        }
    }
}
