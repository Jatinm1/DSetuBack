using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace DealerSetu_Data.Common
{
    public class ValidationHelper
    {
        public ServiceResponse ValidatePagination(int pageIndex, int pageSize)
        {
            if (pageIndex < 1 || pageSize < 1)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = "Invalid pagination parameters.",
                    Message = "PageIndex and PageSize must be greater than zero.",
                    Code = "400",
                    Status = "Error"
                };
            }
            return null;
        }

        public ServiceResponse ValidateDateRange(DateTime? from, DateTime? to)
        {
            if (from.HasValue && to.HasValue && to.Value < from.Value)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = "Invalid date range.",
                    Message = "'To' date cannot be earlier than 'From' date.",
                    Code = "400",
                    Status = "Error"
                };
            }

            return null;
        }

        public ServiceResponse ValidateRequestNo(string requestNo)
        {
            if (!string.IsNullOrWhiteSpace(requestNo) && requestNo.Length > 50)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = "Request number is too long.",
                    Message = "Request number should not exceed 50 characters.",
                    Code = "400",
                    Status = "Error"
                };
            }
            return null;
        }

        public ServiceResponse ValidateInput(string input)
        {
            if (!string.IsNullOrWhiteSpace(input) && input.Length > 30)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = "Input is too long.",
                    Message = "Input should not exceed 30 characters.",
                    Code = "400",
                    Status = "Error"
                };
            }
            return null;
        }

        public ServiceResponse ValidateFYear(string input)
        {
            if (!string.IsNullOrWhiteSpace(input) && input.Length > 4)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = "FYear is too long.",
                    Message = "Fyear should not exceed 4 characters.",
                    Code = "400",
                    Status = "Error"
                };
            }
            return null;
        }

        // New comprehensive validation method for DemoTractorRequestModel
        public ServiceResponse ValidateDemoTractorRequest(DemoTractorRequestModel request)
        {
            if (request == null)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = "Invalid payload",
                    Message = "Request body cannot be null.",
                    Code = "400",
                    Status = "Error"
                };
            }
            if (request.Export == false)
            {
                // Chain validation methods and return first error encountered
                return ValidatePagination((int)request.PageIndex, (int)request.PageSize) ??
                       ValidateDateRange(request.From, request.To) ??
                       ValidateInput(request.State) ??
                       ValidateInput(request.Status) ??
                       ValidateRequestNo(request.RequestNo) ??
                       ValidateFYear(request.Fyear);
            }
            else
            {  // Chain validation methods and return first error encountered
                return ValidateDateRange(request.From, request.To) ??
                       ValidateInput(request.State) ??
                       ValidateInput(request.Status) ??
                       ValidateRequestNo(request.RequestNo) ??
                       ValidateFYear(request.Fyear);

            }
        }

        public ServiceResponse ValidateNewDealerApprovedRequest(ClaimReqModel request)
        {
            if (request == null)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = "Invalid payload",
                    Message = "Request body cannot be null.",
                    Code = "400",
                    Status = "Error"
                };
            }
            if (request.Export == false)
            {
                // Chain validation methods and return first error encountered
                return ValidatePagination((int)request.PageIndex, (int)request.PageSize) ??
                       ValidateDateRange(request.From, request.To) ??
                       ValidateInput(request.State) ??
                       ValidateInput(request.Status) ??
                       ValidateInput(request.ClaimNo);
            }
            else {
                return ValidateDateRange(request.From, request.To) ??
                       ValidateInput(request.State) ??
                       ValidateInput(request.Status) ??
                       ValidateInput(request.ClaimNo);
            }
        }

        public ServiceResponse ValidateNewDealerPendingRequest(PendingClaimReqModel request)
        {
            if (request == null)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = "Invalid payload",
                    Message = "Request body cannot be null.",
                    Code = "400",
                    Status = "Error"
                };
            }

            // Chain validation methods and return first error encountered
            return ValidatePagination(request.PageIndex, request.PageSize) ??
                   ValidateDateRange(request.From, request.To) ??
                   ValidateInput(request.ClaimNo);
        }



        public ServiceResponse ValidateLoginPayload(EncryptedPayload payload)
        {
            if (payload == null ||
                string.IsNullOrEmpty(payload.EncryptedKey) ||
                string.IsNullOrEmpty(payload.EncryptedIV) ||
                string.IsNullOrEmpty(payload.EncryptedData))
            {
                return new ServiceResponse
                {
                    Status = "Failure",
                    Code = "400",
                    Message = "Missing required encryption parameters"
                };
            }
            return null;
        }

        public ServiceResponse ValidateAESKeyAndIV(byte[] aesKey, byte[] aesIv)
        {
            if (aesKey.Length != 32 || aesIv.Length != 16)
            {
                return new ServiceResponse
                {
                    Status = "Failure",
                    Code = "400",
                    Message = "Invalid key or IV length"
                };
            }
            return null;
        }

        public ServiceResponse ValidateLoginModel(LoginModel model)
        {
            if (model == null ||
                string.IsNullOrEmpty(model.EmpNo) ||
                string.IsNullOrEmpty(model.Password) ||
                model.EmpNo.Length > 100 ||
                model.Password.Length > 100)
            {
                return new ServiceResponse
                {
                    Status = "Failure",
                    Code = "400",
                    Message = "Invalid login data"
                };
            }
            return null;
        }

        public ServiceResponse ValidateModelState(ModelStateDictionary modelState)
        {
            if (!modelState.IsValid)
            {
                return new ServiceResponse
                {
                    Status = "Failure",
                    Code = "400",
                    Message = "Invalid request model"
                };
            }
            return null;
        }
    }
}