using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web.Mvc;
using SmartStore.Core.Domain.Common;
using SmartStore.Core.Domain.Customers;
using SmartStore.Core.Domain.Seo;
using SmartStore.Core.Email;
using SmartStore.Services.Catalog;
using SmartStore.Services.Customers;
using SmartStore.Services.Localization;
using SmartStore.Services.Messages;
using SmartStore.Services.Search;
using SmartStore.Services.Seo;
using SmartStore.Services.Topics;
using SmartStore.Web.Framework.Controllers;
using SmartStore.Web.Framework.Security;
using SmartStore.Web.Models.Common;
using SmartStore.Web.Framework.Filters;
using System.Net.Mail;
using SmartStore.Services;

namespace SmartStore.Web.Controllers
{
    public partial class HomeController : PublicControllerBase
	{
		private readonly Lazy<ICategoryService> _categoryService;
		private readonly Lazy<IProductService> _productService;
		private readonly Lazy<IManufacturerService> _manufacturerService;
		private readonly Lazy<ICatalogSearchService> _catalogSearchService;
		private readonly Lazy<CatalogHelper> _catalogHelper;
		private readonly Lazy<ITopicService> _topicService;
		private readonly Lazy<IXmlSitemapGenerator> _sitemapGenerator;
		private readonly Lazy<CaptchaSettings> _captchaSettings;
		private readonly Lazy<CommonSettings> _commonSettings;
		private readonly Lazy<SeoSettings> _seoSettings;
		private readonly Lazy<CustomerSettings> _customerSettings;
		private readonly Lazy<PrivacySettings> _privacySettings;
        private readonly IEmailSender _emailSender;
        private readonly IEmailAccountService _emailAccountService;
        private readonly ICommonServices _services;

        public HomeController(
			Lazy<ICategoryService> categoryService,
			Lazy<IProductService> productService,
			Lazy<IManufacturerService> manufacturerService,
			Lazy<ICatalogSearchService> catalogSearchService,
			Lazy<CatalogHelper> catalogHelper,
			Lazy<ITopicService> topicService,
			Lazy<IXmlSitemapGenerator> sitemapGenerator,
			Lazy<CaptchaSettings> captchaSettings,
			Lazy<CommonSettings> commonSettings,
			Lazy<SeoSettings> seoSettings,
			Lazy<CustomerSettings> customerSettings,
			Lazy<PrivacySettings> privacySettings,
            IEmailSender emailSender,
            IEmailAccountService emailAccountService,
            ICommonServices services)
        {
			_categoryService = categoryService;
			_productService = productService;
			_manufacturerService = manufacturerService;
			_catalogSearchService = catalogSearchService;
			_catalogHelper = catalogHelper;
			_topicService = topicService;
			_sitemapGenerator = sitemapGenerator;
			_captchaSettings = captchaSettings;
			_commonSettings = commonSettings;
			_seoSettings = seoSettings;
            _customerSettings = customerSettings;
			_privacySettings = privacySettings;
            _emailSender = emailSender;
            _emailAccountService = emailAccountService;
            _services = services;

        }
		
        [RequireHttpsByConfig(SslRequirement.No)]
        public ActionResult Index()
        {
			return View();
        }

		public ActionResult StoreClosed()
		{
			return View();
		}

		[RequireHttpsByConfig(SslRequirement.No)]
		[GdprConsent]
		public ActionResult ContactUs()
		{
            var topic = _topicService.Value.GetTopicBySystemName("ContactUs");

            var model = new ContactUsModel
			{
				Email = Services.WorkContext.CurrentCustomer.Email,
				FullName = Services.WorkContext.CurrentCustomer.GetFullName(),
				FullNameRequired = _privacySettings.Value.FullNameOnContactUsRequired,
				DisplayCaptcha = _captchaSettings.Value.Enabled && _captchaSettings.Value.ShowOnContactUsPage,
                MetaKeywords = topic?.GetLocalized(x => x.MetaKeywords),
                MetaDescription = topic?.GetLocalized(x => x.MetaDescription),
                MetaTitle = topic?.GetLocalized(x => x.MetaTitle),
            };

			return View(model);
		}

		[HttpPost, ActionName("ContactUs")]
		[ValidateCaptcha, ValidateHoneypot]
		[GdprConsent]
		public ActionResult ContactUsSend(ContactUsModel model, bool captchaValid)
		{
			// Validate CAPTCHA
			if (_captchaSettings.Value.Enabled && _captchaSettings.Value.ShowOnContactUsPage && !captchaValid)
			{
				ModelState.AddModelError("", T("Common.WrongCaptcha"));
			}

			if (ModelState.IsValid)
			{
				var customer = Services.WorkContext.CurrentCustomer;
				var email = model.Email.Trim();
				var fullName = model.FullName;
				var subject = T("ContactUs.EmailSubject", Services.StoreContext.CurrentStore.Name);
				var body = Core.Html.HtmlUtils.FormatText(model.Enquiry, false, true, false, false, false, false);

				// Required for some SMTP servers
				EmailAddress sender = null;
				if (!_commonSettings.Value.UseSystemEmailForContactUsForm)
				{
					sender = new EmailAddress(email, fullName);
				}

				// email
				var msg = Services.MessageFactory.SendContactUsMessage(customer, email, fullName, subject, body, sender);

				if (msg?.Email?.Id != null)
				{
					model.SuccessfullySent = true;
					model.Result = T("ContactUs.YourEnquiryHasBeenSent");
					Services.CustomerActivity.InsertActivity("PublicStore.ContactUs", T("ActivityLog.PublicStore.ContactUs"));
				}
				else
				{
					ModelState.AddModelError("", T("Common.Error.SendMail"));
					model.Result = T("Common.Error.SendMail");
				}

				return View(model);
			}

			model.DisplayCaptcha = _captchaSettings.Value.Enabled && _captchaSettings.Value.ShowOnContactUsPage;
			return View(model);
		}

		[RequireHttpsByConfigAttribute(SslRequirement.No)]
		public ActionResult SitemapSeo(int? index = null)
		{
			if (!_seoSettings.Value.XmlSitemapEnabled)
				return HttpNotFound();
			
			string content = _sitemapGenerator.Value.GetSitemap(index);

			if (content == null)
			{
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Sitemap index is out of range.");
			}

			return Content(content, "text/xml", Encoding.UTF8);
		}

		[RequireHttpsByConfigAttribute(SslRequirement.No)]
		public ActionResult Sitemap()
		{
            return RedirectPermanent(Services.StoreContext.CurrentStore.Url);
		}

        public ActionResult CompanyInfo()
        {
            return View();
        }

        public ActionResult Disclaimer()
        {
            return View();
        }

        public ActionResult TermsConditions()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Contact(string email, string phone, string comments)
        {
            MailMessage mail = new MailMessage();
           // SmtpClient SmtpServer = new SmtpClient("mail.2xprime.com");

           // SmtpServer.Port = 25;
            //SmtpServer.EnableSsl = true;
            string bodyForAccDept = string.Empty;
            mail.Subject = "Petal Price";
           

            StringBuilder stringBuilder = new StringBuilder(mail.Body);

            stringBuilder.Append("<html><body>");
            stringBuilder.Append("Hello, ");
            stringBuilder.Append("<br/>");
            stringBuilder.Append("<br/>");
            stringBuilder.Append("Please contact to following person.");
            stringBuilder.Append("<br/>");
            stringBuilder.Append("Phone Number:" + phone); //form.phoneNumber
            stringBuilder.Append("<br/>");
            stringBuilder.Append("Email ID: " + email);
            stringBuilder.Append("<br/>");
            stringBuilder.Append("Comments: "+ comments);
            stringBuilder.Append("<br/>");
            stringBuilder.Append("<br/>");
            stringBuilder.Append("<br/>");

            stringBuilder.Append("Thanks, ");
            stringBuilder.Append("<br/>");
            stringBuilder.Append("Petal Price.");
            stringBuilder.Append("</body></html>");

            //SmtpServer.UseDefaultCredentials = false;
            //SmtpServer.Credentials = new System.Net.NetworkCredential("rdudukulu@primetgi.com", "Prime@123");
            //mail.From = new MailAddress("maarafiq@gmail.com");
            // mail.To.Add(new MailAddress("rdudukulu@primetgi.com"));
            mail.Body = stringBuilder.ToString();
            string adminEmail = _services.WorkContext.CurrentCustomer.Email;

            var emailAccount = _emailAccountService.GetEmailAccountById(1); //replace it with Admin Id in future
            var msg = new EmailMessage(adminEmail, mail.Subject, mail.Body, emailAccount.Email);
            _emailSender.SendEmail(new SmtpContext(emailAccount), msg);

           // SmtpServer.Send(mail);
            return Json(true,JsonRequestBehavior.AllowGet);
        }

        public ActionResult FAQ()
        {
            return View();
        }

        public ActionResult ReturnPolicy()
        {
            return View();
        }

        public ActionResult PrivacyPolicy()
        {
            return View();
        }
    }
}
