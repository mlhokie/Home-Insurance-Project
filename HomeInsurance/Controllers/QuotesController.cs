using System;
using HomeInsurance.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace HomeInsurance.Controllers
{
	public class QuotesController : HIControllerBase
	{
		public QuotesController() { }

		public QuotesController(IQuotesEntitySource dataSource): base(dataSource) { }

		public ActionResult GetStarted()
		{
			return View();
		}

		// Full summary of Quote/Property/Location
		public ActionResult QuoteSummary(int? quoteId)
		{
			if (!quoteId.HasValue)
			{
				try
				{
					quoteId = (int)Session["quoteId"];
				}

				catch
				{
					// TODO: redirect
					return RedirectToAction("GetStarted", "Quotes");
				}
			}

			else
			{
				Session["quoteId"] = quoteId;
			}

			// TODO: do we have a User?
			using(IQuotesEntity qe = QuoteSource.CreateQuotesEntity())
			{
				Quote q = qe.IncludeInQuotes("Property.Location.Homeowner.User").Where(qq => qq.Id == quoteId).FirstOrDefault();
				// TODO: is q null?

				//Added this section in order to stop the user from being able to buy same quote multiple times
				List<Policy> policies = new List<Policy>();
				User user = Session["User"] as User;
				Homeowner ho = qe.Homeowners.FirstOrDefault(h => h.UserId == user.Id);
				policies.AddRange(qe.IncludeInPolicies("Quote.Property.Location.Homeowner.User")
					.Where(p => p.Quote.Property.Location.Homeowner.UserId == ho.UserId && p.QuoteId==q.Id));
				bool isPolicyExist = policies.Count > 0;
				Tuple<Quote, bool> returningParameters = new Tuple<Quote, bool>(q, isPolicyExist);//Tuple used for multiple returns
				return View(returningParameters);

				//return View(q); This not needed anymore, q returned in line 55 via Tuple
			}
		}

		public ActionResult QuoteDetails()
		{
			User user = Session["User"] as User;
			List<Quote> quoteList = new List<Quote>();
			using(IQuotesEntity qe = QuoteSource.CreateQuotesEntity())
			{
				quoteList.AddRange(qe.IncludeInQuotes("Property.Location.Homeowner.User")
					.Where(q => q.Property.Location.Homeowner.UserId == user.Id));
				return View(quoteList);
			}
		}

		public ActionResult CoverageDetails()
		{
			Property property = Session["Property"] as Property;
			Quote quote = new Quote(property);

			Session["Quote"] = quote;
			return View(quote);
		}

		public ActionResult SaveQuote()
		{
			Quote quote = Session["Quote"] as Quote;
			Property property = Session["Property"] as Property;
			Location location = Session["Location"] as Location;
			Homeowner homeowner = Session["Homeowner"] as Homeowner;
			User user = Session["User"] as User;

			quote.Property = property;
			property.Location = location;
			location.Homeowner = homeowner;

			homeowner.UserId = user.Id;
			using(IQuotesEntity qe = QuoteSource.CreateQuotesEntity())
			{
				qe.AddQuote(quote);
				qe.SaveChanges();
			}

			Session.Clear();
			Session["User"] = user;
			return RedirectToAction("QuoteDetails");
		}
	}
}