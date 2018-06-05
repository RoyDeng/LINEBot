using LINEBot.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace LINEBot.Controllers
{
    public class BotsController : Controller
    {
        private DatabaseContext db = new DatabaseContext();

        public ActionResult Index()
        {
            if (Session["MemberId"] != null)
            {
                int memberId = (int)Session["MemberId"];
                List<Bot> bots = db.Bots.Where(x => x.MemberId.Equals(memberId)).ToList();
                return View(bots);
            }
            else
            {
                return RedirectToAction("Login", "Auth");
            }
        }

        public ActionResult Details(int? id)
        {
            if (Session["MemberId"] != null)
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                Bot bot = db.Bots.Find(id);
                List<Message> messages = db.Messages.Where(x => x.BotId == id).ToList();
                if (bot == null)
                {
                    return HttpNotFound();
                }
                return View(Tuple.Create(bot, messages));
            }
            else
            {
                return RedirectToAction("Login", "Auth");
            }
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MemberId,ChannelToken,ChannelSecret")] Bot bot)
        {
            if (ModelState.IsValid)
            {
                db.Bots.Add(bot);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(bot);
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Bot bot = db.Bots.Find(id);
            if (bot == null)
            {
                return HttpNotFound();
            }
            return View(bot);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "BotId,ChannelToken,ChannelSecret")] Bot bot)
        {
            if (ModelState.IsValid)
            {
                db.Entry(bot).State = EntityState.Modified;
                db.Entry(bot).Property(x => x.MemberId).IsModified = false;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(bot);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Bot bot = db.Bots.Find(id);
            if (bot == null)
            {
                return HttpNotFound();
            }
            return View(bot);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Bot bot = db.Bots.Find(id);
            db.Bots.Remove(bot);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}