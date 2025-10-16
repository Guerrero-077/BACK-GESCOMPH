using Entity.DTOs.Base;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebGESCOMPH.Filters
{
    // Filtro global: si la acción devuelve PagedResult<T>, escribe headers de paginación.
    public sealed class PagedResultHeadersFilter : IAsyncResultFilter
    {
        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is ObjectResult objectResult && objectResult.Value is IPagedResult paged)
            {
                var headers = context.HttpContext.Response.Headers;

                if (!headers.ContainsKey("X-Total-Count"))
                    headers["X-Total-Count"] = paged.Total.ToString();
                if (!headers.ContainsKey("X-Total-Pages"))
                    headers["X-Total-Pages"] = paged.TotalPages.ToString();
                if (!headers.ContainsKey("X-Page"))
                    headers["X-Page"] = paged.Page.ToString();
                if (!headers.ContainsKey("X-Size"))
                    headers["X-Size"] = paged.Size.ToString();
            }

            await next();
        }
    }
}

