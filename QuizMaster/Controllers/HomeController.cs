using QuizMaster.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Data.Entity.Validation;
using System.Diagnostics;
using Microsoft.Ajax.Utilities;
using QuizApp.Models;

namespace QuizMaster.Controllers
{
    public class HomeController : Controller
    {
        QuizMasterEntities db = new QuizMasterEntities();
        int scoreExam = 0;
        score sc = new score();//ye abhi banaya hau ibject class ka 

        [HttpGet]
        public ActionResult TLogin()
        {
            return View();
        }
        [HttpPost]
        public ActionResult TLogin(tbl_admin admin)
        {

            tbl_admin a = db.tbl_admin.Where(x => x.ad_name == admin.ad_name && x.ad_pass == admin.ad_pass).SingleOrDefault();
            if(a != null)
            {
                Session["ad_id"] = admin.ad_id + 1;
                return RedirectToAction("Dashboard");
            }
            else
            {
                ViewBag.msg = "Invalid User Name Or Password";
            }
            return View();
        }

        [HttpGet]
        public ActionResult SLogin()
        {

            return View();
        }

        [HttpPost]
        public ActionResult SLogin(student student)
        {
            student s = db.students.Where(x => x.std_name == student.std_name && x.std_password == student.std_password).SingleOrDefault();
            if(s != null)
            {
                Session["std_id"] = student.std_id;
                return RedirectToAction("ExamDashboard");   
            }
            else
            {
                ViewBag.msg = "Invalid Id and Password";
            }
            return View();
        }

        public ActionResult Dashboard()
        {
            return View();
        }
        [HttpGet]
        public ActionResult Add_Category()
        {
            
            int ad_id = Convert.ToInt32(Session["ad_id"]);
            List<tbl_category> categories = db.tbl_category.Where(X => X.cat_fk_ad_id == ad_id).OrderByDescending(x=>x.cat_id).ToList();
            ViewData["list"] = categories;
            ViewBag.id = ad_id;
            return View();
        }

        [HttpPost]
        public ActionResult Add_Category(tbl_category cat)
        {
            
            List<tbl_category> categories = db.tbl_category.OrderByDescending(x => x.cat_id).ToList();
            ViewData["list"] = categories;
            Random r = new Random();
            tbl_category c = new tbl_category();
            c.cat_name = cat.cat_name;
            c.cat_fk_ad_id = Convert.ToInt32(Session["ad_id"]);
            c.cat_encrytped_string = encrypt.Encrypt(cat.cat_name.Trim() + r.Next().ToString(), true);
            db.tbl_category.Add(c);
            db.SaveChanges();


            return RedirectToAction("Add_Category");
        }

        [HttpGet]
        public ActionResult Add_Questions()
        {
            
            int ad_id = Convert.ToInt32(Session["ad_id"]);
            List<tbl_category> categories = db.tbl_category.Where(x => x.cat_fk_ad_id == ad_id).ToList();
            ViewBag.list = new SelectList(categories, "cat_id", "cat_name");
            return View();
        }
        [HttpPost]
        public ActionResult Add_Questions(tbl_questions questions)
        {
            
            int ad_id = Convert.ToInt32(Session["ad_id"]);
            List<tbl_category> categories = db.tbl_category.Where(x => x.cat_fk_ad_id == ad_id).ToList();
            ViewBag.list = new SelectList(categories, "cat_id", "cat_name");

            tbl_questions qa = new tbl_questions();
            qa.q_text = questions.q_text;
            qa.QA = questions.QA;
            qa.QB = questions.QB;
            qa.QC = questions.QC;
            qa.QD = questions.QD;
            qa.QCorrectAns = questions.QCorrectAns;
            qa.q_fk_catid = questions.q_fk_catid;

            db.tbl_questions.Add(qa);
            db.SaveChanges();
            TempData["ms"] = "Question Added Successfully";
            TempData.Keep();
            return RedirectToAction("Add_Questions");
        }

        public ActionResult Index()
        {
            if (Session["ad_id"] != null)
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }
        public ActionResult Logout()
        {
            Session.Abandon();
            Session.RemoveAll();

            return RedirectToAction("Index");
        }
        [HttpGet]
        public ActionResult SRegister()
        {
            return View();
        }

        [HttpPost]
        public ActionResult SRegister(student student, HttpPostedFileBase imgfile)
        {
                student stu = new student();
            
                
                stu.std_name = student.std_name;
                stu.std_password = student.std_password;
                stu.std_image = student.std_image;
                db.students.Add(student);
                db.SaveChanges();
                                
                
            return RedirectToAction("SLogin");
        }
        public ActionResult ExamDashboard()
        {
            if (Session["std_id"] == null)
            {
                return RedirectToAction("SLogin");
            }
            return View();
        }
        

        [HttpPost]
        public ActionResult ExamDashboard(string room)
        {

            List<tbl_category> list = db.tbl_category.ToList();
            foreach (var item in list)
            {

                if (item.cat_encrytped_string == room)
                {

                    List<tbl_questions> li = db.tbl_questions.Where(x => x.q_fk_catid == item.cat_id).ToList();
                    Queue<tbl_questions> queue = new Queue<tbl_questions>();
                    foreach (tbl_questions a in li)
                    {
                        queue.Enqueue(a);
                    }


                    TempData["examid"] = item.cat_fk_ad_id;
                    TempData["questions"] = queue;


                    TempData.Keep();
                    return RedirectToAction("StartQuiz");
                }
                else
                {
                    ViewBag.error = "No Room found...";
                }

            }

            return View();
        }

        

        public ActionResult StartQuiz()
        {

            if (Session["std_id"] == null)
            {
                return RedirectToAction("slogin");

            }


            tbl_questions q = null;
            if (TempData["questions"] != null)
            {
                Queue<tbl_questions> qlist = (Queue<tbl_questions>)TempData["questions"];
                if (qlist.Count > 0)
                {
                    q = qlist.Peek();
                    qlist.Dequeue();

                    TempData["questions"] = qlist;
                    TempData.Keep();
                }
                else
                {
                    TempData["score"] = 4;
                    TempData.Keep();
                    return RedirectToAction("EndExam");
                }
            }

            else
            {
                return RedirectToAction("ExamDashboard");

            }

            TempData.Keep();
            return View(q);
        }


        public ActionResult ViewAllQuestions(int? id)
        {
            if (Session["ad_id"] == null)
            {

                return RedirectToAction("TLogin");
            }

            if (id == null)
            {
                return RedirectToAction("Dashboard");
            }



            return View(db.tbl_questions.Where(x => x.q_fk_catid == id).ToList());

        }

        public ActionResult EndExam(tbl_questions q)
        {
            return View();
        }

        [HttpPost]

        public ActionResult StartQuiz(tbl_questions q)
        {

            string correctans = null;

            if (q.QA != null)
            {

                correctans = "A";

            }
            else if (q.QB != null)
            {
                correctans = "B";


            }
            else if (q.QC != null)
            {
                correctans = "C";



            }
            else if (q.QD != null)
            {
                correctans = "D";


            }
            Response.Write("<script>alert('Correct " + correctans + "'); </script>");
            if (correctans.Equals(q.QCorrectAns))
            {
                
                scoreExam = scoreExam + 1;
            }


            TempData.Keep();


            sc.scoree[2] = scoreExam.ToString();

            return RedirectToAction("StartQuiz");
        }


        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}