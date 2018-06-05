using LINEBot.Models;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace LINEBot.Controllers
{
    public class AuthController : Controller
    {
        private DatabaseContext db = new DatabaseContext();

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login([Bind(Include = "Email,Password")] Member member)
        {
            if (ModelState.IsValid)
            {
                var obj = db.Members.Where(x => x.Email.Equals(member.Email) && x.Password.Equals(member.Password)).FirstOrDefault();
                if (obj != null)
                {
                    Session["MemberId"] = obj.MemberId;
                    Session["Email"] = obj.Email.ToString();
                    Session["FirstName"] = obj.FirstName.ToString();
                    Session["LastName"] = obj.LastName.ToString();
                    return RedirectToAction("Index", "Bots");
                }
                else
                {
                    return View(member);
                }
            }

            return View(member);
        }

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register([Bind(Include = "Email,Password,FirstName,LastName")] Member member)
        {
            if (ModelState.IsValid)
            {
                db.Members.Add(member);
                db.SaveChanges();
                return RedirectToAction("Login");
            }

            return View(member);
        }

        public ActionResult Edit()
        {
            int memberId = (int)Session["MemberId"];
            Member member = db.Members.Find(memberId);
            if (member == null)
            {
                return HttpNotFound();
            }
            return View(member);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MemberId,FirstName,LastName")] Member member)
        {
            if (ModelState.IsValid)
            {
                db.Entry(member).State = EntityState.Modified;
                db.Entry(member).Property(x => x.Email).IsModified = false;
                db.Entry(member).Property(x => x.Password).IsModified = false;
                db.SaveChanges();
                Session["FirstName"] = member.FirstName.ToString();
                Session["LastName"] = member.LastName.ToString();
                return View(member);
            }
            return View(member);
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}