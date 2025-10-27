using APICatalogo.DTOs;
using APICatalogo.DTOs.Mappings;
using APICatalogo.Filters;
using APICatalogo.Pagination;
using APICatalogo.Repositories;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace APICatalogo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriasController : ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly ILogger _logger;

        public CategoriasController(ILogger<CategoriasController> logger, IUnitOfWork uof)
        {
            _logger = logger;
            _uof = uof;
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

        [HttpGet]
        [ServiceFilter(typeof(ApiLoggingFilter))]
        public ActionResult<IEnumerable<CategoriaDTO>> Get()
        {
           var categorias = _uof.CategoriaRepository.GetAll();
           
            if(categorias is null)
                return NotFound("Não existem categorias...");

            var categoriasDto = new List<CategoriaDTO>();
            foreach(var categoria in categorias)
            {
                var categoriaDto = new CategoriaDTO
                {
                    CategoriaId = categoria.CategoriaId,
                    Nome = categoria.Nome,
                    ImagemUrl = categoria.ImagemUrl
                };
                categoriasDto.Add(categoriaDto);
            }

            return Ok(categoriasDto);
        }

        [HttpGet("pagination")]
        public ActionResult<IEnumerable<CategoriaDTO>> Get([FromQuery] CategoriasParameters categoriasParameters)
        {
            var categorias = _uof.CategoriaRepository.GetCategorias(categoriasParameters);
            
            return ObterCategorias(categorias);
        }

        [HttpGet("filter/nome/pagination")]
        public ActionResult<IEnumerable<CategoriaDTO>> GetCategoriasFiltradas([FromQuery] CategoriasFiltroNome categoriasFiltro)
        {
            var categoriasFiltradas = _uof.CategoriaRepository.GetCategoriasFiltroNome(categoriasFiltro);

            return ObterCategorias(categoriasFiltradas);
        }

        private ActionResult<IEnumerable<CategoriaDTO>> ObterCategorias(PagedList<Models.Categoria> categorias)
        {
            var metadata = new
            {
                categorias.TotalCount,
                categorias.PageSize,
                categorias.CurrentPage,
                categorias.TotalPages,
                categorias.HasNext,
                categorias.HasPrevious
            };

            Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(metadata));

            var categoriasDto = categorias.ToCategoriaDTOList();

            return Ok(categoriasDto);
        }

        [HttpGet("{id:int}", Name = "ObterCategoria")]
        public ActionResult<CategoriaDTO> Get(int id)
        {

            var categoria = _uof.CategoriaRepository.Get(c => c.CategoriaId == id);

            if (categoria is null)
            {
                _logger.LogInformation($"=========================Get api/categorias/id = {id} NOT FOUND ================");
                return NotFound("Categoria não encontrado...");
            }

            var categoriaDto = new CategoriaDTO()
            {
                CategoriaId = categoria.CategoriaId,
                Nome = categoria.Nome,
                ImagemUrl = categoria.ImagemUrl
            };

            return Ok(categoriaDto);
        }


        [HttpPost]
        public ActionResult<CategoriaDTO> Post(CategoriaDTO categoriaDto)
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
            _uof.Commit();

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
        public ActionResult<CategoriaDTO> Put(int id, CategoriaDTO categoriaDto)
        {
            if (id != categoriaDto.CategoriaId)
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
            _uof.Commit();

            var novaCategoriaAtualizada = new CategoriaDTO()
            {
                CategoriaId = categoria.CategoriaId,
                Nome = categoria.Nome,
                ImagemUrl = categoria.ImagemUrl
            };

            return Ok(novaCategoriaAtualizada);
        }

        [HttpDelete("{id:int}")]
        public ActionResult<CategoriaDTO> Delete(int id)
        {
            var categoria = _uof.CategoriaRepository.Get(c => c.CategoriaId == id);

            if (categoria is null)
            {
                return NotFound("Categoria não localizado...");
            }

            var categoriaExcluida = _uof.CategoriaRepository.Delete(categoria);
            _uof.Commit();

            var novaCategoriaExcluida = new CategoriaDTO()
            {
                CategoriaId = categoria.CategoriaId,
                Nome = categoria.Nome,
                ImagemUrl = categoria.ImagemUrl
            };

            return Ok(novaCategoriaExcluida);


        }
    }
}





