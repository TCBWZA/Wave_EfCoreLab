using EfCoreLab.DTOs;
using EfCoreLab.Mappings;
using EfCoreLab.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EfCoreLab.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(
            IInvoiceRepository invoiceRepository,
            ICustomerRepository customerRepository,
            ILogger<InvoicesController> logger)
        {
            _invoiceRepository = invoiceRepository;
            _customerRepository = customerRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAll()
        {
            var invoices = await _invoiceRepository.GetAllAsync();
            return Ok(invoices.Select(i => i.ToDto()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceDto>> GetById(long id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null)
                return NotFound($"Invoice with ID {id} not found.");

            return Ok(invoice.ToDto());
        }

        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetByCustomerId(long customerId)
        {
            if (!await _customerRepository.ExistsAsync(customerId))
                return NotFound($"Customer with ID {customerId} not found.");

            var invoices = await _invoiceRepository.GetByCustomerIdAsync(customerId);
            return Ok(invoices.Select(i => i.ToDto()));
        }

        [HttpGet("number/{invoiceNumber}")]
        public async Task<ActionResult<InvoiceDto>> GetByInvoiceNumber(string invoiceNumber)
        {
            var invoice = await _invoiceRepository.GetByInvoiceNumberAsync(invoiceNumber);
            if (invoice == null)
                return NotFound($"Invoice with number {invoiceNumber} not found.");

            return Ok(invoice.ToDto());
        }

        [HttpPost]
        public async Task<ActionResult<InvoiceDto>> Create([FromBody] CreateInvoiceDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if customer exists
            if (!await _customerRepository.ExistsAsync(dto.CustomerId))
                return BadRequest($"Customer with ID {dto.CustomerId} does not exist.");

            // Check if invoice number already exists
            if (await _invoiceRepository.InvoiceNumberExistsAsync(dto.InvoiceNumber))
                return Conflict($"An invoice with number {dto.InvoiceNumber} already exists.");

            var invoice = dto.ToEntity();
            var created = await _invoiceRepository.CreateAsync(invoice);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.ToDto());
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<InvoiceDto>> Update(long id, [FromBody] UpdateInvoiceDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null)
                return NotFound($"Invoice with ID {id} not found.");

            // Check if invoice number already exists for another invoice
            if (await _invoiceRepository.InvoiceNumberExistsAsync(dto.InvoiceNumber, id))
                return Conflict($"An invoice with number {dto.InvoiceNumber} already exists.");

            dto.UpdateEntity(invoice);
            var updated = await _invoiceRepository.UpdateAsync(invoice);

            return Ok(updated.ToDto());
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(long id)
        {
            var deleted = await _invoiceRepository.DeleteAsync(id);
            if (!deleted)
                return NotFound($"Invoice with ID {id} not found.");

            return NoContent();
        }
    }
}
