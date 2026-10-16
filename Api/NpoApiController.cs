using ElamIgugu.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ElamIgugu.Web.Api;

[Route("api/npo")]
public class NpoApiController : ApiControllerBase
{
    private readonly NpoOptions _npo;
    public NpoApiController(IOptions<NpoOptions> npo) => _npo = npo.Value;

    /// <summary>The NPO details, bank details and the donation choices shown on the app's Donate screen.</summary>
    [HttpGet("contact")]
    public ActionResult<ApiNpoResponse> GetContact() => new ApiNpoResponse
    {
        Slogan = _npo.Slogan,
        RegistrationNumber = _npo.RegistrationNumber,
        InstagramHandle = _npo.InstagramHandle,
        Email = _npo.Email,
        PhoneNumbers = _npo.PhoneNumbers,
        BankName = _npo.Bank.BankName,
        BankAccountName = _npo.Bank.AccountName,
        BankAccountNumber = _npo.Bank.AccountNumber,
        ItemTypes = DonationOptions.ItemTypes.ToList(),
        Cities = DonationOptions.Cities.ToList(),
        MoneyItemType = DonationOptions.Money
    };
}
