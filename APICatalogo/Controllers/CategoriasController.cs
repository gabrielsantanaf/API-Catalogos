using APICatalogo.DTOs;
using APICatalogo.DTOs.Mappings;
using APICatalogo.Filters;
using APICatalogo.Pagination;
using APICatalogo.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Newtonsoft.Json;
using X.PagedList;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using APICatalogo.Context;
using APICatalogo.Models;

namespace APICatalogo.Controllers
{
    [EnableCors("OriginsComAcessoPermitido")]
    [EnableRateLimiting("fixedwindow")]
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class CategoriasController : ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly ILogger _logger;
        private readonly IMemoryCache _cache;
        private const string CacheCategoriasKey = "CacheCategorias";

        public CategoriasController(ILogger<CategoriasController> logger, IUnitOfWork uof, IMemoryCache cache)
        {
            _logger = logger;
            _uof = uof;
            _cache = cache;
        }

        //[HttpGet("UsandoFromService/{nome}")]
        //public ActionResult<string> GetSaudacaoFromServices([FromServices] IMeuServico meuServico,
        //                                                     string nome)
        //{
        //    return meuServico.Saudacao(nome);
        //}

        //[HttpGet("SemUsarFromService/{nome}")]
        //public ActionResult<string> GetSaudacaoSemFromServices(IMeuServico meuServico,
        //                                                    string nome)
        //{
        //    return meuServico.Saudacao(nome);
        //}

        //[HttpGet("produtos")]
        //public async Task<ActionResult<IEnumerable<Categoria>>> GetCategoriasProdutos()
        //{
        //    _logger.LogInformation("===============Get api/categorias/produtos===================");

        //    return _context.Categorias.AsNoTracking().Include(p => p.Produtos).Where(c => c.CategoriaId <= 5).ToListAsync();
        //    //return _context.Categorias.Include(p => p.Produtos).ToList();

        //}

        /// <summary>
        /// Obtem uma lista de objetos Categoria
        /// </summary>
        /// <returns>Uma lista de objetos Categoria</returns>
        [HttpGet]
        [ServiceFilter(typeof(ApiLoggingFilter))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<IEnumerable<CategoriaDTO>>> Get()
        { 
            if (! _cache.TryGetValue(CacheCategoriasKey, out IEnumerable<Categoria>? categorias))
            {
                categorias = await _uof.CategoriaRepository.GetAllAsync();

                if (categorias is null || !categorias.Any())
                {
                    _logger.LogWarning("Não existem categorias");
                   return NotFound("Não existem categorias...");                  
                }

                SetCache(CacheCategoriasKey, categorias);
            }

             var categoriasDto = categorias.ToCategoriaDTOList();
             return Ok(categoriasDto);
        }

        
        [HttpGet("pagination")]
        public async Task<ActionResult<IEnumerable<CategoriaDTO>>> Get([FromQuery] CategoriasParameters categoriasParameters)
        {
            var categorias = await _uof.CategoriaRepository.GetCategoriasAsync(categoriasParameters);
            
            return ObterCategorias(categorias);
        }

        [HttpGet("filter/nome/pagination")]
        public async Task<ActionResult<IEnumerable<CategoriaDTO>>> GetCategoriasFiltradas([FromQuery] CategoriasFiltroNome categoriasFiltro)
        {
            var categoriasFiltradas = await _uof.CategoriaRepository.GetCategoriasFiltroNomeAsync(categoriasFiltro);

            return ObterCategorias(categoriasFiltradas);
        }

        private ActionResult<IEnumerable<CategoriaDTO>> ObterCategorias(IPagedList<Models.Categoria> categorias)
        {
            var metadata = new
            {
                categorias.Count,
                categorias.PageSize,
                categorias.PageCount,
                categorias.TotalItemCount,
                categorias.HasNextPage,
                categorias.HasPreviousPage
            };

            Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(metadata));

            var categoriasDto = categorias.ToCategoriaDTOList();

            return Ok(categoriasDto);
        }

        /// <summary>
        /// Obtem uma categoria pelo seu ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Objetos Categoria</returns>
        [DisableCors]
        [HttpGet("{id:int}", Name = "ObterCategoria")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CategoriaDTO>> Get(int id)
        {
            var cacheKey = GetCategoriaCacheKey(id);

            if ( ! _cache.TryGetValue(cacheKey, out Categoria? categoria))
            {
                categoria = await _uof.CategoriaRepository.GetAsync(c => c.CategoriaId == id);

                if (categoria is null)
                {
                    _logger.LogInformation($"=========================Get api/categorias/id = {id} NOT FOUND ================");
                    return NotFound("Categoria não encontrado...");                    
                }
                SetCache(cacheKey, categoria);
            }

            var categoriaDto = new CategoriaDTO()
            {
                CategoriaId = categoria.CategoriaId,
                Nome = categoria.Nome,
                ImagemUrl = categoria.ImagemUrl
            };

            return Ok(categoriaDto);
        }

        /// <summary>
        /// Inclui uma nova categoria
        /// </summary>
        /// <remarks>
        /// Exemplo de request: 
        /// 
        ///     POST api/categorias
        ///     {
        ///         "CategoriaId": 1,
        ///         "nome": "categoria1",
        ///         "imagemUrl": "http://teste.net/1.jpg"
        ///     }
        /// </remarks>
        /// <param name="categoriaDto"></param>
        /// <returns>O objeto Categoria incluído</returns>
        /// <remarks>Retorna um objetos categoria incluído</remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<CategoriaDTO>> Post(CategoriaDTO categoriaDto)
        {

            if (categoriaDto is null)
            {
                return BadRequest();
            }

            var categoria = new Models.Categoria()
            {
                CategoriaId = categoriaDto.CategoriaId,
                Nome = categoriaDto.Nome,
                ImagemUrl = categoriaDto.ImagemUrl
            };

            var categoriaCriada = _uof.CategoriaRepository.Create(categoria);
            await _uof.CommitAsync();

            InvalidateCacheAfterChange(categoriaCriada.CategoriaId, categoriaCriada);

            var novaCategoria = new CategoriaDTO()
            {
                CategoriaId = categoria.CategoriaId,
                Nome = categoria.Nome,
                ImagemUrl = categoria.ImagemUrl
            };

            return new CreatedAtRouteResult("ObterCategoria",
                new { id = novaCategoria.CategoriaId }, novaCategoria);

        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<CategoriaDTO>> Put(int id, CategoriaDTO categoriaDto)
        {
            if (id <= 0 || id != categoriaDto.CategoriaId)
            {
                return BadRequest();
            }

            var categoria = new Models.Categoria()
            {
                CategoriaId = categoriaDto.CategoriaId,
                Nome = categoriaDto.Nome,
                ImagemUrl = categoriaDto.ImagemUrl
            };

            var categoriaAtualizada = _uof.CategoriaRepository.Update(categoria);
            await _uof.CommitAsync();

            InvalidateCacheAfterChange(id, categoriaAtualizada);

            var novaCategoriaAtualizada = new CategoriaDTO()
            {
                CategoriaId = categoria.CategoriaId,
                Nome = categoria.Nome,
                ImagemUrl = categoria.ImagemUrl
            };

            return Ok(novaCategoriaAtualizada);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<CategoriaDTO>> Delete(int id)
        {
            var categoria = await _uof.CategoriaRepository.GetAsync(c => c.CategoriaId == id);

            if (categoria is null)
            {
                return NotFound("Categoria não localizado...");
            }

            var categoriaExcluida = _uof.CategoriaRepository.Delete(categoria);
            await _uof.CommitAsync();

            InvalidateCacheAfterChange(id);

            var novaCategoriaExcluida = new CategoriaDTO()
            {
                CategoriaId = categoria.CategoriaId,
                Nome = categoria.Nome,
                ImagemUrl = categoria.ImagemUrl
            };

            return Ok(novaCategoriaExcluida);
        }

        private string GetCategoriaCacheKey(int id) => $"CacheCategoria_{id}";

        private void SetCache<T>(string key, T data)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30),
                SlidingExpiration = TimeSpan.FromSeconds(15),
                Priority = CacheItemPriority.High
            };
            _cache.Set(key, data, cacheOptions);
        }

        private void InvalidateCacheAfterChange(int id, Categoria? categoria = null)
        {
            _cache.Remove(CacheCategoriasKey);
            _cache.Remove(GetCategoriaCacheKey(id));

            if (categoria != null)
            {
                SetCache(GetCategoriaCacheKey(id), categoria);
            }
        }
    }
}





