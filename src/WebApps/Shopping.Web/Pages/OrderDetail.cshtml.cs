using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shopping.Web.Models.Ordering;
using Shopping.Web.Services;

namespace Shopping.Web.Pages;

public class OrderDetailModel : PageModel
{
    private readonly IOrderingService _orderingService;
    private readonly ILogger<OrderDetailModel> _logger;

    public OrderDetailModel(IOrderingService orderingService, ILogger<OrderDetailModel> logger)
    {
        _orderingService = orderingService;
        _logger = logger;
    }

    public OrderModel Order { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(Guid orderId)
    {
        try
        {
            if (orderId == Guid.Empty)
            {
                _logger.LogWarning("Empty order ID provided");
                return NotFound();
            }

            // Siparişi doğrudan ID ile al
            var response = await _orderingService.GetOrdersByCustomer(Guid.Parse("1")); // Test için sabit bir customer ID
            if (response == null || response.Orders == null)
            {
                _logger.LogWarning("No orders found for customer");
                return NotFound();
            }

            var order = response.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
            {
                _logger.LogWarning("Order not found with ID: {OrderId}", orderId);
                return NotFound();
            }

            Order = order;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting order details for ID: {OrderId}", orderId);
            TempData["ErrorMessage"] = "An error occurred while retrieving the order details. Please try again later.";
            return RedirectToPage("/Error");
        }
    }
} 