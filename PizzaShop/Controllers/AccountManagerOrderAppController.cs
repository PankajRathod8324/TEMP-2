using System.Diagnostics;
using System.Security.Claims;
using DAL.Interfaces;
using Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entities.ViewModel;
using X.PagedList.Extensions;
using BLL.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Text.Json.Nodes;
using System.Linq;
using DocumentFormat.OpenXml.Office.CustomUI;

namespace PizzaShop.Controllers;


public class AccountManagerOrderAppController : Controller
{
    // private readonly ILogger<HomeController> _logger;

    private readonly IAccountManagerOrderAppService _accountmanagerorderapp;
    private readonly IMenuService _menuService;
    private readonly IOrderService _orderService;

    private readonly ITaxService _taxService;
    private readonly ITableAndSectionService _tableandsectionService;


    public AccountManagerOrderAppController(IAccountManagerOrderAppService accountmanagerorderapp, ITableAndSectionService tableandsectionService, IMenuService menuService, IOrderService orderService, ITaxService taxService)
    {
        _menuService = menuService;
        _accountmanagerorderapp = accountmanagerorderapp;
        _tableandsectionService = tableandsectionService;
        _orderService = orderService;
        _taxService = taxService;
    }

    [Authorize(Policy = "AccountManagerPolicy")]
    public IActionResult Table()
    {
        var sections = _tableandsectionService.GetAllSection();
        ViewBag.Sections = sections.Select(r => new SelectListItem
        {
            Value = r.SectionId.ToString(),
            Text = r.SectionName
        }).ToList();
        var section = _accountmanagerorderapp.GetOrderSection();
        return View(section);
    }

    [Authorize(Policy = "AccountManagerPolicy")]
    public IActionResult GetWaitingList(int sectionId)
    {
        var sections = _tableandsectionService.GetAllSection();
        ViewBag.Sections = sections.Select(r => new SelectListItem
        {
            Value = r.SectionId.ToString(),
            Text = r.SectionName
        }).ToList();
        Console.WriteLine("Hi i am in Water");
        var waitinglist = _accountmanagerorderapp.GetAllWaitingListCustomer(sectionId);
        return PartialView("_WaitingList", waitinglist);
    }

    [Authorize(Policy = "AccountManagerPolicy")]
    public IActionResult GetWaitingData(int waitingId)
    {

        var sections = _tableandsectionService.GetAllSection();
        ViewBag.Sections = sections.Select(r => new SelectListItem
        {
            Value = r.SectionId.ToString(),
            Text = r.SectionName
        }).ToList();
        var waitingData = _accountmanagerorderapp.GetWaitingData(waitingId);
        return PartialView("_CustomerDetailsForm", waitingData);
    }

    public IActionResult GetWaitingDataByEmail(string email)
    {

        var sections = _tableandsectionService.GetAllSection();
        ViewBag.Sections = sections.Select(r => new SelectListItem
        {
            Value = r.SectionId.ToString(),
            Text = r.SectionName
        }).ToList();
        var waitingData = _accountmanagerorderapp.GetWaitingDataByEmail(email);
        return PartialView("_CustomerDetailsForm", waitingData);
    }

    public IActionResult AddCustomerDetail([FromBody] JsonObject customerdata)
    {
        bool success = _accountmanagerorderapp.AddOrUpdateCustomer(customerdata);
        string email = (string)customerdata["email"];
        var customerId = _accountmanagerorderapp.GetCustomerIDByName(email);
        if (success)
        {
            return Json(new { success = true, message = "Customer Added Successfully!", customerId = customerId });
        }
        else
        {
            return Json(new { success = false, message = "An error occurred while processing the request." });
        }
    }

    public IActionResult GetWaitingToken(string email)
    {
        var sections = _tableandsectionService.GetAllSection();
        ViewBag.Sections = sections.Select(r => new SelectListItem
        {
            Value = r.SectionId.ToString(),
            Text = r.SectionName
        }).ToList();
        var waitingData = _accountmanagerorderapp.GetWaitingDataByEmail(email);
        return PartialView("_WaitingTokenDetailsPV", waitingData);
    }

    public IActionResult AddInWaitingList([FromBody] JsonObject customerdata)
    {
        bool success = _accountmanagerorderapp.AddInWaitingList(customerdata);

        if (success)
        {
            return Json(new { success = true, message = "Customer Added Successfully!" });
        }
        else
        {
            return Json(new { success = false, message = "An error occurred while processing the request." });
        }
    }
    [Authorize(Policy = "ChefOrAccountManagerPolicy")]
    public IActionResult KOT()
    {
        var categories = _menuService.GetAllCategories();
        var categoryvm = new MenuCategoryVM
        {
            CategoryName = categories.FirstOrDefault().CategoryName,
            menuCategories = categories
        };
        return View(categoryvm);
    }

    public IActionResult GetKOT(int categoryId, string? status)
    {
        List<OrderVM> category = _accountmanagerorderapp.GetOrderCategoryItem(categoryId, status);
        return PartialView("_KOTCard", category);
    }

    public IActionResult GetOrderDetails([FromBody] List<OrderVM> orderDetailsArray)
    {
        return PartialView("_KOTPrepare", orderDetailsArray);
    }

    public IActionResult MakePrepare([FromBody] List<PrepareItemVM> prepare)
    {
        bool success = _accountmanagerorderapp.UpdateOrderStatus(prepare);

        if (success)
        {
            return Json(new { success = true, message = "Order Status Updated Successfully!" });
        }
        else
        {
            return Json(new { success = false, message = "An error occurred while processing the request." });
        }
    }

    [Authorize(Policy = "AccountManagerPolicy")]
    public IActionResult WaitingList()
    {
        var sections = _tableandsectionService.GetAllSection();
        var sectionvm = sections.Select(Section => new SectionVM
        {
            SectionId = Section.SectionId,
            SectionName = Section.SectionName,
            WaitingList = _accountmanagerorderapp.GetAllWaitingListCustomer(Section.SectionId).Count()
        }).ToList();
        return View(sectionvm);
    }

    public IActionResult GetWaitingListBySectionId(int sectionId)
    {
        var sections = _tableandsectionService.GetAllSection();
        ViewBag.Sections = sections.Select(r => new SelectListItem
        {
            Value = r.SectionId.ToString(),
            Text = r.SectionName
        }).ToList();
        // var tables = _tableandsectionService.Get
        var waitingData = _accountmanagerorderapp.GetAllWaitingListCustomer(sectionId);
        return PartialView("_WaitingListPV", waitingData);
    }

    // public IActionResult GetTableBySectionId(int sectionId)
    // {
    //     var tables = _tableandsectionService.GetAvailableTablesBySectionId(sectionId);
    // }

    public JsonResult GetTableListBySection(int sectionId)
    {
        var tables = _tableandsectionService.GetAvailableTablesBySectionId(sectionId);
        return Json(tables);

    }

    [Authorize(Policy = "AccountManagerPolicy")]
    public IActionResult MenuOrderApp(int customerId)
    {
        var categories = _menuService.GetAllCategories();
        ViewBag.CustomerId = customerId;
        var tables = _accountmanagerorderapp.GetTablesByCustomerId(customerId);
        var tax = _taxService.GetAllTaxes();


        var categoryvm = new MenuCategoryVM
        {
            CategoryName = categories.FirstOrDefault()?.CategoryName,
            customerTables = tables,
            menuCategories = categories,
            Taxes = tax
        };

        return View(categoryvm);
    }

    public IActionResult GetItemByCategory(int CategoryId)
    {
        var items = _menuService.GetItemByCategoryId(CategoryId);
        return PartialView("_MenuItemCard", items);
    }

    public IActionResult AddToFavouriteItem(int ItemId)
    {
        bool success = _accountmanagerorderapp.AddToFavouriteItem(ItemId);

        if (success)
        {
            return Json(new { success = true, message = "Item Added to Favourites!" });
        }
        else
        {
            return Json(new { success = false, message = "An error occurred while processing the request." });
        }
    }
    public IActionResult GetFavouriteItem()
    {
        var favouriteItems = _accountmanagerorderapp.GetFavouriteItem();
        return PartialView("_MenuItemCard", favouriteItems);
    }

    public IActionResult DeleteWaitingToken(int waitingId)
    {
        bool success = _accountmanagerorderapp.DeleteWaitingToken(waitingId);

        if (success)
        {
            return Json(new { success = true, message = "Waiting Token Deleted Successfully!" });
        }
        else
        {
            return Json(new { success = false, message = "An error occurred while processing the request." });
        }
    }

    public IActionResult AssignTable(int waitingId, int tableId)
    {
        bool success = _accountmanagerorderapp.AssignTable(waitingId, tableId);
        // var customer = _accountmanagerorderapp.  
        if (success)
        {
            return Json(new { success = true, message = "Table Assigned Successfully!" });
        }
        else
        {
            return Json(new { success = false, message = "An error occurred while processing the request." });
        }
    }

    public IActionResult GetModifierFromItem(int ItemId)
    {
        var combinedModifier = _accountmanagerorderapp.GetItemModifier(ItemId);
        return PartialView("_MenuModifierPV", combinedModifier);
    }

    public IActionResult GetMenuItemDetails([FromBody] List<MenuCategoryVM> ItemDetailsArray)
    {
        var finalItemData = _accountmanagerorderapp.SetOrderItemData(ItemDetailsArray);
        return PartialView("_OrderDataPV", finalItemData);
    }

    public IActionResult GetCustomerDetails(int CustomerId)
    {
        var personaldata = _accountmanagerorderapp.GetCustomerDetails(CustomerId);
        return PartialView("_PersonalData", personaldata);
    }

    public IActionResult EditCustomer(CustomerVM customerdata)
    {
        bool success = _accountmanagerorderapp.EditCustomer(customerdata);
        if (success)
        {
            return Json(new { success = true, message = "Table Assigned Successfully!" });
        }
        else
        {
            return Json(new { success = false, message = "An error occurred while processing the request." });
        }
    }









    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}