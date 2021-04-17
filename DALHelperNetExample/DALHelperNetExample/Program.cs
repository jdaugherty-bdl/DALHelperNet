using DALHelperNetExample.Examples;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNetExample
{
    class Program
    {
        /// <summary>
        /// This program will simulate as many DALHelperNet calls as possible. All code inside will give errors unless your database is set up properly.
        /// </summary>
        static void Main(string[] args)
        {
            bool success;

            // run all the examples that return a DataTable
            success = DataTableExamples.RunAllDataTableExamples();

            // run all the examples that return a DTO
            success = DTOExamples.RunAllDTOExamples();
        }
    }
}
