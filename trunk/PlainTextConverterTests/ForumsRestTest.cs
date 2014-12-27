using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CommunityBridge3.ForumsRestService;

namespace PlainTextConverterTests
{
    [TestClass]
    public class ForumsRestTest
    {
        //[TestMethod]
        //public void TestMethod1()
        //{
        //    var rest = new ServiceAccess("tZNt5SSBt1XPiWiueGaAQMnrV4QelLbm7eum1750GI4=");
        //    rest.GetForums(forums =>
        //        {
        //            foreach (Forum f in forums)
        //            {
        //                Console.WriteLine("{1} - {0}", f.DisplayName, f.Locale);
        //                if (f.Threads > 0)
        //                {
        //                    rest.GetThreads(f.Id, threads =>
        //                        {
        //                            foreach (Thread t in threads)
        //                            {
        //                                Console.WriteLine("  {0} - {1} - {2}", t.Created, t.CreatedBy.DisplayName, t.Title);
        //                                if (t.RepliesCount > 0)
        //                                {
        //                                    rest.GetThreadReplies(t.Id, replies =>
        //                                        {
        //                                            foreach (ThreadReply r in replies)
        //                                            {
        //                                                Console.WriteLine("  {0} - {1}", r.Created, r.CreatedBy.DisplayName);
        //                                            }
        //                                        });
        //                                }
        //                            }
        //                        });
        //                }
        //            }
        //        });
        //}

        [TestMethod]
        public void TestMethod1()
        {
            var dict = new Dictionary<string, Forum>(StringComparer.OrdinalIgnoreCase);
            using (var file = new StreamWriter("forums.txt"))
            {
                var rest = new ServiceAccess("tZNt5SSBt1XPiWiueGaAQMnrV4QelLbm7eum1750GI4=", null);
                rest.GetForums(forums =>
                    {
                        foreach (Forum f in forums)
                        {
                            //file.WriteLine("{1} - {0} - {2} - {3} - {4}", f.Name, f.Locale, f.Type, string.Join("|", f.Brands), string.Join("|", f.Categories.Select(p => p.Name + "(" + p.Brand + "|" + p.Locale + ")")));

                            //dict.Add(f.Locale + "." + f.Name, f);

                            bool added = false;
                            if (f.Brands.Count > 0)
                            {
                                foreach (var c in f.Brands)
                                {
                                    file.WriteLine("{3} {0}.{1}.{2}", c, f.Locale, f.Name, f.Id);
                                    added = true;
                                }
                            }
                            if (added == false)
                            {
                                file.WriteLine("{3} {0}.{1}.{2}", "Unknown", f.Locale, f.Name, f.Id);
                            }
                            if (f.Type != "Forum") Console.WriteLine();
                            //if (f.Brands.Count <= 0)
                            //{
                            //    Console.WriteLine("{1} - {0} - {2}", f.Name, f.Locale, f.DisplayName);
                            //}

                            if (f.Name.IndexOf("mvpnntpanswersbridge", StringComparison.OrdinalIgnoreCase) >= 0)
                            {

                                Console.WriteLine("{1} - {0} - {2}", f.Name, f.Locale, f.DisplayName);
                            }
                        }
                    });
            }
        }

        [TestMethod]
        public void TestReply()
        {
            //Guid forumTestId = new Guid("2a5bd289-1569-4b7b-b2f9-a69cb11ea6f0");
            var rest = new ServiceAccess("tZNt5SSBt1XPiWiueGaAQMnrV4QelLbm7eum1750GI4=", null);
            //var threads = rest.GetThreads(forumTestId);
            Guid threadId = new Guid("42fe437d-9f92-4302-80c9-2bbbbabb131a");
            rest.PostReply(threadId, "TEST");

        }
    }
}

