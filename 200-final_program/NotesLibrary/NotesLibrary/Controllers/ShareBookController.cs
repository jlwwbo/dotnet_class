﻿using Microsoft.AspNet.Identity;
using NotesLibrary.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ViewModel;
using Verification;

namespace NotesLibrary.Controllers
{
    public class ShareBookController : Controller
    {
        public ActionResult Index(int BookId, int NoteId)
        {
            using (LibraryDBContext db = new LibraryDBContext())
            {
                string UserId = System.Web.HttpContext.Current.User.Identity.GetUserId();
                Models.User user = db.Users.Find(UserId);
                if (user == null)
                    return View(new ViewModel.ShareBookViewModel { HasLogin = false });
                var noteInfo = db.NoteInfoes.Find(BookId, user.Id);
                if (noteInfo == null || noteInfo.Id != NoteId)
                    return View(new ViewModel.ShareBookViewModel { HasLogin = true, HasBook = false });
                var shareLines = db.Shares.Where(b => b.BookId == BookId && b.UserId == user.Id).ToList();
                // var shareLines = db.Shares.Where(b => b.UserId == user.Id).ToList();
                // var shareLines = from s in db.Shares where s.BookId == BookId && s.UserId == user.Id select s;
                int shareCount = 0;
                var shareInfoes = new List<ViewModel.ShareInfo>();
                foreach (var shareLine in shareLines)
                {
                    shareCount++;
                    string shareLink = GenerateShareLink
                            (shareLine.BookId, shareLine.UserId, shareLine.VerifyCode, true);
                    string hasImplemented = "No";
                    string implementName = "";
                    if (shareLine.ImplementId != null)
                    {
                        hasImplemented = "Yes";
                        var implementUser = db.Users.Find(shareLine.ImplementId);
                        implementName = implementUser.UserName;
                    }
                    shareInfoes.Add(new ViewModel.ShareInfo
                    {
                        Index = shareCount,
                        ShareLink = shareLink,
                        HasImplemented = hasImplemented,
                        ImplementName = implementName
                    });
                }
                return View(new ViewModel.ShareBookViewModel
                {
                    HasLogin = true,
                    HasBook = true,
                    BookName = db.BookInfoes.Find(BookId).Name,
                    ShareInfoes = shareInfoes,
                    BookId = BookId,
                    NoteId = NoteId
                });
            }
        }
        public string GenerateShareLink(int BookId, string UserId, string VerifyCode, bool IsIndex)
        {
            string url = Request.Url.ToString();
            int divideIndex;
            if (IsIndex)
                divideIndex = url.LastIndexOf('?');
            else
                divideIndex = url.LastIndexOf('/');
            string prefix = url.Substring(0, divideIndex);
            string suffix = "/Share?BookId=" + BookId.ToString()
                + "&UserId=" + UserId + "&VerifyCode=" + VerifyCode;
            return prefix + suffix;
        }
        public string GenerateVerifyCode(int BookId, string UserId)
        {
            string BookIdStr = BookId.ToString();
            string rawText = BookIdStr + UserId + BookIdStr;
            string verifyCode;
            verifyCode = md5.GetVerifyCode(rawText);
            // verifyCode = sha256.GetVerifyCode(rawText);
            return verifyCode;
        }
        public ActionResult Create(int BookId, int NoteId)
        {
            using (LibraryDBContext db = new LibraryDBContext())
            {
                string UserId = System.Web.HttpContext.Current.User.Identity.GetUserId();
                Models.User user = db.Users.Find(UserId);
                if (user == null)
                    return View(new ViewModel.ShareBookViewModel { HasLogin = false });
                var noteInfo = db.NoteInfoes.Find(BookId, user.Id);
                if (noteInfo == null || noteInfo.Id != NoteId)
                    return View(new ViewModel.ShareBookViewModel { HasLogin = true, HasBook = false });
                string verifyCode = GenerateVerifyCode(BookId, user.Id);
                db.Shares.Add(new Models.ShareLine
                {
                    VerifyCode = verifyCode,
                    BookId = BookId,
                    UserId = user.Id
                });
                db.SaveChanges();
                string shareLink = GenerateShareLink(BookId, user.Id, verifyCode, false);
                return View(new ViewModel.CreateShareViewModel
                {
                    HasLogin = true,
                    HasBook = true,
                    BookName = db.BookInfoes.Find(BookId).Name,
                    NewShareLink = shareLink,
                    BookId = BookId,
                    NoteId = NoteId
                });
            }
        }
        public ActionResult Share(int BookId, string UserId, string VerifyCode)
        {
            using (LibraryDBContext db = new LibraryDBContext())
            {
                string implementId = System.Web.HttpContext.Current.User.Identity.GetUserId();
                Models.User implementUser = db.Users.Find(implementId);
                if (implementUser == null)
                    return View(new ViewModel.ShareInfoViewModel { HasLogin = false });
                var shareInfo = db.Shares.Find(VerifyCode);
                if (shareInfo == null || shareInfo.UserId != UserId || shareInfo.BookId != BookId)
                    return View(new ViewModel.ShareInfoViewModel { HasLogin = true, CurrectLink = false });
                if (shareInfo.ImplementId != null)
                    return View(new ViewModel.ShareInfoViewModel { HasLogin = true, CurrectLink = true, NotUsed = false });

                var noteInfo = db.NoteInfoes.Find(BookId, UserId);
                string bookIdStr = BookId.ToString();
                string noteIdStr = noteInfo.Id.ToString();
                string[] books = implementUser.Books.Split(',');
                string[] notes = implementUser.Notes.Split(',');
                bool hasBook = false;
                for (int i = 0; i < books.Length; i++)
                    if (books[i] == bookIdStr && notes[i] == noteIdStr)
                    {
                        hasBook = true;
                        break;
                    }
                if (hasBook)
                    return View(new ViewModel.ShareInfoViewModel { HasLogin = true, CurrectLink = true, NotUsed = true, NotHasBook = false });
                
                if (implementUser.Books != "")
                {
                    implementUser.Books += ",";
                    implementUser.Notes += ",";
                }
                implementUser.Books += bookIdStr;
                implementUser.Notes += noteIdStr;
                db.Users.AddOrUpdate(implementUser);
                shareInfo.ImplementId = implementId;
                db.Shares.AddOrUpdate(shareInfo);
                db.SaveChanges();
                return View(new ViewModel.ShareInfoViewModel
                {
                    HasLogin = true,
                    CurrectLink = true,
                    NotUsed = true,
                    NotHasBook = true,
                    BookName = db.BookInfoes.Find(BookId).Name,
                    OwnerName = db.Users.Find(noteInfo.OwnerId).UserName
                });
            }
        }
    }
}