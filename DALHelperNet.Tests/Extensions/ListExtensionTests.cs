using DALHelperNet.Extensions;
using Newtonsoft.Json;

namespace DALHelperNet.Tests.Extensions
{
    public class ListExtensionTests
    {
        public class ListMock
        {
            public int Property1 { get; set; }
            public List<ListMock>? Children { get; set; }
        }

        public ListMock SimpleMockedList { get; set; }
        public IEnumerable<ListMock> SimpleExpectedList { get; private set; }

        public ListMock MockedList { get; set; }
        public IEnumerable<ListMock> ExpectedList { get; private set; }

        [SetUp]
        public void Setup()
        {
            SimpleMockedList = new ListMock
            {
                Property1 = 0,
            };

            SimpleExpectedList = new List<ListMock> 
            { 
                new ListMock 
                { 
                    Property1 = 0 
                } 
            };

            MockedList = new ListMock
            {
                Property1 = 0,
                Children = new List<ListMock>
                {
                    new ListMock
                    {
                        Property1 = 1,
                        Children = new List<ListMock>
                        {
                            new ListMock
                            {
                                Property1 = 3
                            },
                            new ListMock
                            {
                                Property1 = 4
                            }
                        }
                    },
                    new ListMock
                    {
                        Property1 = 2,
                        Children = new List<ListMock>
                        {
                            new ListMock
                            {
                                Property1 = 5
                            }
                        }
                    }
                }
            };

            ExpectedList = new List<ListMock>
            {
                new ListMock
                {
                    Property1 = 0,
                    Children = new List<ListMock>
                    {
                        new ListMock
                        {
                            Property1 = 1,
                            Children = new List<ListMock>
                            {
                                new ListMock
                                {
                                    Property1 = 3
                                },
                                new ListMock
                                {
                                    Property1 = 4
                                }
                            }
                        },
                        new ListMock
                        {
                            Property1 = 2,
                            Children = new List<ListMock>
                            {
                                new ListMock
                                {
                                    Property1 = 5
                                }
                            }
                        }
                    }
                },
                new ListMock
                {
                    Property1 = 1,
                    Children = new List<ListMock>
                    {
                        new ListMock
                        {
                            Property1 = 3
                        },
                        new ListMock
                        {
                            Property1 = 4
                        }
                    }
                },
                new ListMock
                {
                    Property1 = 3
                },
                new ListMock
                {
                    Property1 = 4
                },
                new ListMock
                {
                    Property1 = 2,
                    Children = new List<ListMock>
                    {
                        new ListMock
                        {
                            Property1 = 5
                        }
                    }
                },
                new ListMock
                {
                    Property1 = 5
                }
            };
        }

        [Test]
        public void FlattenSimpleTreeObjectTest()
        {
            var result = ListExtensions.FlattenTreeObject(new List<ListMock> { SimpleMockedList }, x => x.Children);

            StringAssert.AreEqualIgnoringCase(JsonConvert.SerializeObject(result?.ToList()), JsonConvert.SerializeObject(SimpleExpectedList));
        }

        [Test]
        public void FlattenTreeObjectTest()
        {
            var result = ListExtensions.FlattenTreeObject(new List<ListMock> { MockedList }, x => x.Children);

            StringAssert.AreEqualIgnoringCase(JsonConvert.SerializeObject(result?.ToList()), JsonConvert.SerializeObject(ExpectedList));
        }
    }
}