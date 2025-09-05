using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using APICatalogo.Context;
using APICatalogo.Models;
using APICatalogo.Repositories;
using AutoMapper;
using APICatalogo.DTOs;

namespace APICatalogo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProdutosController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _uof;
        private readonly IMapper _mapper;
        public ProdutosController(IConfiguration configuration, IUnitOfWork uof, IMapper mapper)
        {

            _configuration = configuration;
            _uof = uof;
            _mapper = mapper;
        }

        [HttpGet("produtos/{id}")]
        public ActionResult <IEnumerable<ProdutoDTO>> GetProdutoCategoria(int id)
        {
            var produtos = _uof.ProdutoRepository.GetProdutosPorCategoria(id);

            if (produtos is null)
                return NotFound();

            var produtosDTO = _mapper.Map<IEnumerable<ProdutoDTO>>(produtos);

            return Ok(produtosDTO);
        }

        [HttpGet("LerArquivoConfiguration")]
        public string GetValores()
        {
            var valor1 = _configuration["chave1"];
            var valor2 = _configuration["chave2"];

            var secao1 = _configuration["secao1:chave2"];

            return $"Chave1 = {valor1} \nChave2 = {valor2} \nSeção1 => Chave2 = {secao1}";
        }

        [HttpGet]
        public ActionResult<IEnumerable<ProdutoDTO>> Get()
        {

            var produtos = _uof.ProdutoRepository.GetAll();
            if (produtos is null)
            {
                return NotFound("Produtos não encontrados...");
            }

            var produtosDTO = _mapper.Map<IEnumerable<ProdutoDTO>>(produtos);
            return Ok(produtosDTO);


        }

        [HttpGet("{id:int}", Name = "ObterProduto")]
        public ActionResult<ProdutoDTO> Get(int id)
        {

            var produto = _uof.ProdutoRepository.Get(c => c.ProdutoId == id);  
            if (produto is null)
            {
                return NotFound("Produto não encontrado...");
            }
            var produtoDTO = _mapper.Map<ProdutoDTO>(produto);
            return Ok(produtoDTO);

        }

        [HttpPost]
        public ActionResult<ProdutoDTO> Post(ProdutoDTO produtoDTO)
        {

            if (produtoDTO is null)
            {
                return BadRequest();
            }

            var produto = _mapper.Map<Produto>(produtoDTO);

            var novoProduto = _uof.ProdutoRepository.Create(produto);
            _uof.Commit();

            var novoProdutoDTO = _mapper.Map<ProdutoDTO>(novoProduto);

            return new CreatedAtRouteResult("ObterProduto",
                new { id = novoProduto.ProdutoId }, novoProduto);


        }

        [HttpPut("{id:int}")]
        public ActionResult<ProdutoDTO> Put(int id, ProdutoDTO produtoDTO)
        {

            if (id != produtoDTO.ProdutoId)
                return BadRequest();

            var produto = _mapper.Map<Produto>(produtoDTO);
            var produtoAtualizado = _uof.ProdutoRepository.Update(produto);
            _uof.Commit();

            var produtoAtualizadoDTO = _mapper.Map<Produto>(produtoAtualizado);
            return Ok(produtoAtualizadoDTO);

        }

        [HttpDelete("{id:int}")]
        public ActionResult<ProdutoDTO> Delete(int id)
        {
            var produto = _uof.ProdutoRepository.Get(c => c.ProdutoId == id);
            if (produto is null)
            {
                return NotFound("Produto não encontrado...");
            }
            var produtoDeletado = _uof.ProdutoRepository.Delete(produto);
            _uof.Commit();

            var produtoDeletadoDTO = _mapper.Map<ProdutoDTO>(produtoDeletado);
            return Ok(produtoDeletadoDTO);
        }
    }
}






