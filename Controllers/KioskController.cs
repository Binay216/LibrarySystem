using LibrarySystem.Extensions;
using LibrarySystem.Models.ViewModels;
using LibrarySystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Controllers
{
    /// <summary>
    /// The public self-service kiosk - a fixed terminal at the branch, not a page
    /// on the website, hence [AllowAnonymous] and its own _KioskLayout (no site
    /// nav, no login links, no footer).
    ///
    /// Borrower identification: there's no patron login here - the number a
    /// patron keys in on the card-entry screens is taken directly as the
    /// Borrower.Id. That's a reasonable shortcut for a demo with no real library
    /// cards or patron auth to issue, but it would be the wrong call in a real
    /// deployment: a database row id is sequential and guessable, so anyone who
    /// tries a few numbers can view another patron's loans, fines and
    /// reservations, or borrow against their account. A real system needs a
    /// card number that's a separate, non-sequential field on Borrower (or a PIN
    /// alongside it) rather than exposing the primary key as the credential.
    ///
    /// Everything that needs the card number (checkout, account summary, a hold
    /// placed without already being identified) keeps it server-side in session
    /// only - never in a URL, query string or hidden form field - so it's never
    /// visible in the browser's address bar or history. Session is cleared by
    /// Reset, called from the Cancel button on every screen, the 60s inactivity
    /// timer, and every screen's own completion action - requirement 7.
    /// </summary>
    [AllowAnonymous]
    public class KioskController(ICurrentBranchAccessor currentBranchAccessor, IKioskService kioskService) : Controller
    {
        private const string CardNumberHint = "Enter your card number using the keypad.";
        private const string UnknownCardMessage = "We couldn't find an account for that card number. Please check the number or ask a staff member for help.";

        /// <summary>The idle screen - resets here after 60s of inactivity or when Cancel is pressed anywhere.</summary>
        public async Task<IActionResult> Index()
        {
            var branch = await currentBranchAccessor.GetCurrentBranchAsync();
            return View(model: branch.Name);
        }

        /// <summary>Clears the in-progress transaction and returns to idle - Cancel, timeout, and every "Done" button use this.</summary>
        public IActionResult Reset()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Card-number entry for checking out.</summary>
        [HttpGet]
        public IActionResult CheckOut() => View();

        /// <summary>Identifies the borrower, then moves on to entering library codes.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(string cardNumber)
        {
            if (!int.TryParse(cardNumber, out var card))
            {
                ModelState.AddModelError(string.Empty, CardNumberHint);
                return View();
            }

            var borrower = await kioskService.FindActiveBorrowerAsync(card);
            if (borrower is null)
            {
                ModelState.AddModelError(string.Empty, UnknownCardMessage);
                return View();
            }

            HttpContext.Session.SetObject(KioskSessionState.SessionKey, new KioskSessionState { BorrowerId = borrower.Id });
            return RedirectToAction(nameof(CheckOutItems));
        }

        /// <summary>Library-code entry and the running list of what's been checked out this session.</summary>
        [HttpGet]
        public async Task<IActionResult> CheckOutItems()
        {
            var state = GetSessionState();
            if (state?.BorrowerId is not { } borrowerId)
            {
                return RedirectToAction(nameof(CheckOut));
            }

            var borrower = await kioskService.FindActiveBorrowerAsync(borrowerId);
            if (borrower is null)
            {
                return RedirectToAction(nameof(Reset));
            }

            return View(new KioskCheckOutItemsViewModel
            {
                BorrowerName = $"{borrower.FirstName} {borrower.LastName}",
                CheckedOutItems = state.CheckedOutItems,
                HoldOfferLibraryCode = TempData["HoldOfferLibraryCode"] as string
            });
        }

        /// <summary>Borrows one item via IKioskService/ILoanService and adds it to the session's running list.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOutItems(string libraryCode)
        {
            var state = GetSessionState();
            if (state?.BorrowerId is not { } borrowerId)
            {
                return RedirectToAction(nameof(CheckOut));
            }

            if (string.IsNullOrWhiteSpace(libraryCode))
            {
                TempData["ErrorMessage"] = "Enter a library code using the keypad.";
                return RedirectToAction(nameof(CheckOutItems));
            }

            var attempt = await kioskService.CheckOutItemAsync(libraryCode, borrowerId);
            if (attempt.Succeeded)
            {
                state.CheckedOutItems.Add(attempt.CheckedOutItem!);
                HttpContext.Session.SetObject(KioskSessionState.SessionKey, state);
                TempData["StatusMessage"] = attempt.Message;
            }
            else
            {
                TempData["ErrorMessage"] = attempt.Message;
                TempData["HoldOfferLibraryCode"] = attempt.HoldOfferLibraryCode;
            }

            return RedirectToAction(nameof(CheckOutItems));
        }

        /// <summary>Card-number entry for the account summary.</summary>
        [HttpGet]
        public IActionResult Account() => View();

        /// <summary>Identifies the borrower, then shows their account summary.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Account(string cardNumber)
        {
            if (!int.TryParse(cardNumber, out var card))
            {
                ModelState.AddModelError(string.Empty, CardNumberHint);
                return View();
            }

            var borrower = await kioskService.FindActiveBorrowerAsync(card);
            if (borrower is null)
            {
                ModelState.AddModelError(string.Empty, UnknownCardMessage);
                return View();
            }

            HttpContext.Session.SetObject(KioskSessionState.SessionKey, new KioskSessionState { BorrowerId = borrower.Id });
            return RedirectToAction(nameof(AccountSummary));
        }

        /// <summary>Current loans, outstanding fines and active reservations for the identified borrower.</summary>
        [HttpGet]
        public async Task<IActionResult> AccountSummary()
        {
            var state = GetSessionState();
            if (state?.BorrowerId is not { } borrowerId)
            {
                return RedirectToAction(nameof(Account));
            }

            return View(await kioskService.GetAccountSummaryAsync(borrowerId));
        }

        /// <summary>Searches the catalogue by name or description - no card number needed.</summary>
        [HttpGet]
        public async Task<IActionResult> Search(string? term)
        {
            var results = string.IsNullOrWhiteSpace(term) ? [] : await kioskService.SearchAsync(term);
            return View(new KioskSearchViewModel { Term = term, Results = results });
        }

        /// <summary>
        /// Shows the hold confirmation screen - asking for a card number first if
        /// the patron isn't already identified this session. Never places the hold
        /// itself (that's the POST overload) so reloading this page can't double-submit.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PlaceHold(string libraryCode, string? returnTo)
        {
            var item = await kioskService.FindItemByLibraryCodeAsync(libraryCode);
            if (item is null)
            {
                return NotFound();
            }

            var state = GetSessionState();
            return View(new KioskPlaceHoldViewModel
            {
                LibraryCode = libraryCode,
                ItemName = item.Name,
                NeedsCardNumber = state?.BorrowerId is null,
                ReturnTo = returnTo
            });
        }

        /// <summary>Places the hold via IKioskService/IReservationService, collecting a card number first if needed.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceHold(string libraryCode, string? returnTo, string? cardNumber)
        {
            var item = await kioskService.FindItemByLibraryCodeAsync(libraryCode);
            if (item is null)
            {
                return NotFound();
            }

            var state = GetSessionState();
            int borrowerId;

            if (state?.BorrowerId is { } knownBorrowerId)
            {
                borrowerId = knownBorrowerId;
            }
            else
            {
                if (!int.TryParse(cardNumber, out var card))
                {
                    return View(FailedHoldModel(libraryCode, item.Name, returnTo, CardNumberHint));
                }

                var borrower = await kioskService.FindActiveBorrowerAsync(card);
                if (borrower is null)
                {
                    return View(FailedHoldModel(libraryCode, item.Name, returnTo, UnknownCardMessage));
                }

                borrowerId = borrower.Id;
                // Identified for the rest of this kiosk session, same as checkout/account.
                HttpContext.Session.SetObject(KioskSessionState.SessionKey, new KioskSessionState { BorrowerId = borrowerId });
            }

            var result = await kioskService.PlaceHoldAsync(item.Id, borrowerId);
            return View(new KioskPlaceHoldViewModel
            {
                LibraryCode = libraryCode,
                ItemName = item.Name,
                NeedsCardNumber = false,
                ReturnTo = returnTo,
                ResultMessage = result.Message,
                ResultSucceeded = result.Succeeded
            });
        }

        private static KioskPlaceHoldViewModel FailedHoldModel(string libraryCode, string itemName, string? returnTo, string message) => new()
        {
            LibraryCode = libraryCode,
            ItemName = itemName,
            NeedsCardNumber = true,
            ReturnTo = returnTo,
            ResultMessage = message,
            ResultSucceeded = false
        };

        private KioskSessionState? GetSessionState() => HttpContext.Session.GetObject<KioskSessionState>(KioskSessionState.SessionKey);
    }
}
