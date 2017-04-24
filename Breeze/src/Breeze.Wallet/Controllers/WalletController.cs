﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using Breeze.Wallet.Errors;
using Microsoft.AspNetCore.Mvc;
using Breeze.Wallet.Models;
using Breeze.Wallet.Wrappers;
using NBitcoin;

namespace Breeze.Wallet.Controllers
{
    /// <summary>
    /// Controller providing operations on a wallet.
    /// </summary>
	[Route("api/v{version:apiVersion}/[controller]")]
    public class WalletController : Controller
    {
        private readonly IWalletManager walletManager;

        public WalletController(IWalletManager walletManager)
        {
            this.walletManager = walletManager;
        }

        /// <summary>
        /// Creates a new wallet on the local machine.
        /// </summary>
        /// <param name="request">The object containing the parameters used to create the wallet.</param>
        /// <returns>A JSON object containing the mnemonic created for the new wallet.</returns>
        [Route("create")]
        [HttpPost]
        public IActionResult Create([FromBody]WalletCreationRequest request)
        {
            // checks the request is valid
            if (!this.ModelState.IsValid)
            {
                var errors = this.ModelState.Values.SelectMany(e => e.Errors.Select(m => m.ErrorMessage));
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Formatting error", string.Join(Environment.NewLine, errors));
            }

            try
            {
                // get the wallet folder 
                DirectoryInfo walletFolder = GetWalletFolder(request.FolderPath);
                Mnemonic mnemonic = this.walletManager.CreateWallet(request.Password, walletFolder.FullName, request.Name, request.Network);

                return this.Json(mnemonic.ToString());
            }
            catch (InvalidOperationException e)
            {
                // indicates that this wallet already exists
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.Conflict, "This wallet already exists.", e.ToString());
            }
        }

        /// <summary>
        /// Loads a wallet previously created by the user.
        /// </summary>
        /// <param name="request">The name of the wallet to load.</param>
        /// <returns></returns>
		[Route("load")]
        [HttpPost]
        public IActionResult Load([FromBody]WalletLoadRequest request)
        {
            // checks the request is valid
            if (!this.ModelState.IsValid)
            {
                var errors = this.ModelState.Values.SelectMany(e => e.Errors.Select(m => m.ErrorMessage));
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Formatting error", string.Join(Environment.NewLine, errors));
            }

            try
            {
                // get the wallet folder 
                DirectoryInfo walletFolder = GetWalletFolder(request.FolderPath);

                Wallet wallet = this.walletManager.LoadWallet(request.Password, walletFolder.FullName, request.Name);
                return this.Json(new WalletModel
                {
                    Network = wallet.Network.Name,
                    //	Addresses = wallet.GetFirstNAddresses(10).Select(a => a.ToWif()),
                    FileName = wallet.WalletFilePath
                });
            }
            catch (FileNotFoundException e)
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.NotFound, "This wallet was not found at the specified location.", e.ToString());
            }
            catch (SecurityException e)
            {
                // indicates that the password is wrong
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.Forbidden, "Wrong password, please try again.", e.ToString());
            }
            catch (Exception e)
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        /// Recovers a wallet.
        /// </summary>
        /// <param name="request">The object containing the parameters used to recover a wallet.</param>
        /// <returns></returns>
        [Route("recover")]
        [HttpPost]
        public IActionResult Recover([FromBody]WalletRecoveryRequest request)
        {
            // checks the request is valid
            if (!this.ModelState.IsValid)
            {
                var errors = this.ModelState.Values.SelectMany(e => e.Errors.Select(m => m.ErrorMessage));
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Formatting error", string.Join(Environment.NewLine, errors));
            }

            try
            {
                // get the wallet folder 
                DirectoryInfo walletFolder = GetWalletFolder(request.FolderPath);

                Wallet wallet = this.walletManager.RecoverWallet(request.Password, walletFolder.FullName, request.Name, request.Network, request.Mnemonic);
                return this.Json(new WalletModel
                {
                    Network = wallet.Network.Name,
                    //	Addresses = wallet.GetFirstNAddresses(10).Select(a => a.ToWif()),
                    FileName = wallet.WalletFilePath
                });
            }
            catch (InvalidOperationException e)
            {
                // indicates that this wallet already exists
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.Conflict, "This wallet already exists.", e.ToString());
            }
            catch (FileNotFoundException e)
            {
                // indicates that this wallet does not exist
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.NotFound, "Wallet not found.", e.ToString());
            }
            catch (Exception e)
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        /// Get some general info about a wallet.
        /// </summary>
        /// <param name="model">The name of the wallet.</param>
        /// <returns></returns>
        [Route("general-info")]
        [HttpGet]
        public IActionResult GetGeneralInfo([FromQuery] WalletName model)
        {
            // checks the request is valid
            if (!this.ModelState.IsValid)
            {
                var errors = this.ModelState.Values.SelectMany(e => e.Errors.Select(m => m.ErrorMessage));
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Formatting error", string.Join(Environment.NewLine, errors));
            }

            try
            {
                return this.Json(this.walletManager.GetGeneralInfo(model.Name));

            }
            catch (Exception e)
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        /// Retrieves the history of a wallet.
        /// </summary>
        /// <param name="model">The name of the wallet.</param>
        /// <returns></returns>
		[Route("history")]
        [HttpGet]
        public IActionResult GetHistory([FromQuery] WalletName model)
        {
            // checks the request is valid
            if (!this.ModelState.IsValid)
            {
                var errors = this.ModelState.Values.SelectMany(e => e.Errors.Select(m => m.ErrorMessage));
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Formatting error", string.Join(Environment.NewLine, errors));
            }

            try
            {
                return this.Json(this.walletManager.GetHistory(model.Name));

            }
            catch (Exception e)
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        /// Gets the balance of a wallet.
        /// </summary>
        /// <param name="model">The name of the wallet.</param>
        /// <returns></returns>
		[Route("balance")]
        [HttpGet]
        public IActionResult GetBalance([FromQuery] WalletName model)
        {
            // checks the request is valid
            if (!this.ModelState.IsValid)
            {
                var errors = this.ModelState.Values.SelectMany(e => e.Errors.Select(m => m.ErrorMessage));
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Formatting error", string.Join(Environment.NewLine, errors));
            }

            try
            {
                return this.Json(this.walletManager.GetBalance(model.Name));

            }
            catch (Exception e)
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        /// Builds a transaction. 
        /// </summary>
        /// <param name="request">The transaction parameters.</param>
        /// <returns>All the details of the transaction, including the hex used to execute it.</returns>
		[Route("build-transaction")]
        [HttpPost]
        public IActionResult BuildTransaction([FromBody] BuildTransactionRequest request)
        {
            // checks the request is valid
            if (!this.ModelState.IsValid)
            {
                var errors = this.ModelState.Values.SelectMany(e => e.Errors.Select(m => m.ErrorMessage));
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Formatting error", string.Join(Environment.NewLine, errors));
            }

            try
            {
                return this.Json(this.walletManager.BuildTransaction(request.Password, request.Address, request.Amount, request.FeeType, request.AllowUnconfirmed));

            }
            catch (Exception e)
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        /// Sends a transaction.
        /// </summary>
        /// <param name="request">The hex representing the transaction.</param>
        /// <returns></returns>
		[Route("send-transaction")]
        [HttpPost]
        public IActionResult SendTransaction([FromBody] SendTransactionRequest request)
        {
            // checks the request is valid
            if (!this.ModelState.IsValid)
            {
                var errors = this.ModelState.Values.SelectMany(e => e.Errors.Select(m => m.ErrorMessage));
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Formatting error", string.Join(Environment.NewLine, errors));
            }

            try
            {
                var result = this.walletManager.SendTransaction(request.Hex);
                if (result)
                {
                    return this.Ok();
                }

                return this.StatusCode((int)HttpStatusCode.BadRequest);
            }
            catch (Exception e)
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        /// Lists all the wallet files found under the default folder.
        /// </summary>
        /// <returns>A list of the wallets files found.</returns>
        [Route("files")]
        [HttpGet]
        public IActionResult ListWalletsFiles()
        {
            try
            {
                DirectoryInfo walletsFolder = GetWalletFolder();

                WalletFileModel model = new WalletFileModel
                {
                    WalletsPath = walletsFolder.FullName,
                    WalletsFiles = Directory.EnumerateFiles(walletsFolder.FullName, "*.json", SearchOption.TopDirectoryOnly).Select(p => Path.GetFileName(p))
                };

                return this.Json(model);
            }
            catch (Exception e)
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        /// Gets a folder.
        /// </summary>
        /// <returns>The path folder of the folder.</returns>
        /// <remarks>The folder is created if it doesn't exist.</remarks>
        private static DirectoryInfo GetWalletFolder(string folderPath = null)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = GetDefaultWalletFolderPath();
            }
            return Directory.CreateDirectory(folderPath);
        }

        /// <summary>
        /// Gets the path of the default folder in which the wallets will be stored.
        /// </summary>
        /// <returns>The folder path for Windows, Linux or OSX systems.</returns>
        private static string GetDefaultWalletFolderPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $@"{Environment.GetEnvironmentVariable("AppData")}\Breeze";
            }

            return $"{Environment.GetEnvironmentVariable("HOME")}/.breeze";
        }
    }
}
