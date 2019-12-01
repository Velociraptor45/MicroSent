using MicroSent.Models;
using MicroSent.Models.Analyser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexiconExtension
{
    class Program
    {
        static void Main(string[] args)
        {
            LexiconExtender lexiconExtender = new LexiconExtender();
            lexiconExtender.extract();
        }
    }
}
