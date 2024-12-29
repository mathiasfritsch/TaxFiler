using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TaxFiler.DB;
using TaxFiler.Models;
using TaxFiler.Service;

namespace TaxFiler.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly TaxFilerContext _taxFilerContext;
        private readonly ISyncService _syncService;
        private readonly IGoogleDriveService _googleDriveService;
        private readonly IParseService _parseService;
        private readonly IDocumentService _documentService;
 
        public HomeController(TaxFilerContext taxFilerContext,
            ISyncService syncService,
            IGoogleDriveService googleDriveService,
            IParseService parseService,
            IDocumentService documentService
            )
        {
            _taxFilerContext = taxFilerContext;
            _syncService = syncService;
            _googleDriveService = googleDriveService;
            _parseService = parseService;
            _documentService = documentService;
        }
        
        
        [HttpGet("TestDB")]
        public IActionResult TestDb()
        {
            try
            {
                _taxFilerContext.Database.OpenConnection();
                _taxFilerContext.Database.CloseConnection();
                ViewBag.Message = "Could open and close connection to database";
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ViewBag.Message = e.Message;
            }
            
            return View("TestDB");
        }
        
        [HttpPost("SyncFiles")]
        public async Task<IActionResult> SyncFiles()
        {
            await _syncService.SyncFilesAsync();

            return View();
        }
        
        [HttpDelete("DeleteAll")]
        public async Task<IActionResult> DeleteAllDocuments()
        {
            await _documentService.DeleteAllDocumentsAsync();
            
            return RedirectToAction("Index");
        }

        
        public async Task<IActionResult> GoogleDocuments()
        {
            var viewModel = new HomeViewModel
            {
                Files = await _googleDriveService.GetFilesAsync()
            };

            return View(viewModel);
        }

       
        [HttpPost]
        public async Task<ActionResult> Parse([FromForm] int fileId)
        {
            var parseResult = await _parseService.ParseFilesAsync(fileId);

            if (parseResult.IsFailed)
            {
                TempData["Error"] = parseResult.Errors.First().Message;
            }
            
            return RedirectToAction("Index", "Home");
        }
        
        public async Task<IActionResult> IndexAsync()
        {
            if(TempData["Error"] !=null) ViewBag.Error = TempData["Error"]!;
            
            var documents = await _documentService.GetDocumentsAsync();
            
            var documentViewModel = new DocumentViewModel
            {
                Documents = documents.ToArray()
            };
            
            return View(documentViewModel);
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}