using DALHelperNet;
using DALHelperNet.Extensions;
using DALHelperNet.Interfaces.Attributes;
using DALHelperNetExample.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNetExample.Examples
{
    public class DTOExamples
    {
        /// <summary>
        /// Runs all DALHelperNet examples that return a DataTable.
        /// </summary>
        /// <returns>True/false as to whether or not all runs were successful.</returns>
        public static bool RunAllDTOExamples()
        {
            dynamic dynamicResult; // this is the standard return type for the following functions

            // get a table without passing any parameters
            dynamicResult = GetFullExampleObjects();

            return true;
        }

        public static dynamic GetFullExampleObjects()
        {
            // get data query using the DALTable attribute on the ExampleBasicObject class as a table name
            var exampleQuery = $@"SELECT * FROM {typeof(ExampleObject).GetCustomAttribute<DALTable>()?.TableName ?? "example_objects"}
                WHERE column1 > 0
                AND column2 = 'string value'
                ORDER BY column3;";

            // get the list of example objects
            var exampleObjects = DALHelper.GetDataObjects<ExampleObject>(ExampleConnectionStringTypes.FirstApplicationDatabase, exampleQuery);



            // now we use those objects to generate DTOs

            // this will cause a problem when serializing due to circular DALTransferProperty references between ExampleObject.ExampleChildren and ExampleObjectChild.ExampleParent
            var badDtoConversion = exampleObjects.GenerateDTO();



            // in order to prevent circular reference errors when serializing, you must explicitly break the chain on either the reference from the 
            //   main ExampleObject object, or from each of the child objects referencing back to the main ExampleObject object

            // so you can do either:
            var goodDtoConversion1 = exampleObjects.GenerateDTO(ExcludeProperties: new string[] { "ExampleObject.ExampleChildren" });

            // or you can do:
            var goodDtoConversion2 = exampleObjects.GenerateDTO(ExcludeProperties: new string[] { "ExampleObjectChild.ExampleParent" });



            // we can also include properties that aren't marked with the DALTransferProperty attribute 
            var includeDtoConversion1 = exampleObjects.GenerateDTO(IncludeProperties: new string[] { "ExampleObjectChild.NoDtoProperty" });

            // we can use any level of namespace to reference a property for either inclusion or exclusion
            var namespaceDtoConversion1 = exampleObjects.GenerateDTO(IncludeProperties: new string[] { "DALHelperNetExample.Models.ExampleObjectChild.NoDtoProperty" });
            var namespaceDtoConversion2 = exampleObjects.GenerateDTO(IncludeProperties: new string[] { "Models.ExampleObjectChild.NoDtoProperty" });
            var namespaceDtoConversion3 = exampleObjects.GenerateDTO(IncludeProperties: new string[] { "ExampleObjectChild.NoDtoProperty" });
            
            var namespaceDtoConversion4 = exampleObjects.GenerateDTO(IncludeProperties: new string[] { "NoDtoProperty" });
            // a note on this last one: specifying the property name only will obey the inclusion/exclusion rule on all objects with that property name
            //   for example, the property "NoDtoProperty" is on both "ExampleObject" and "ExampleObjectChild", so that property will be included with both objects

            return goodDtoConversion2;
        }
    }
}
