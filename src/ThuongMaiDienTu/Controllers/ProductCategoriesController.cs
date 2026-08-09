using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Models;

namespace ThuongMaiDienTu.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class ProductCategoriesController : Controller
    {
        private readonly ThuongMaiDienTuDbContext _context;

        public ProductCategoriesController(ThuongMaiDienTuDbContext context)
        {
            _context = context;
        }

        // GET: ProductCategories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.ProductCategories
                .Include(c => c.Parent)
                .AsNoTracking()
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();

            return View(categories);
        }

        // GET: ProductCategories/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.ProductCategories
                .Include(c => c.Parent)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: ProductCategories/Create
        public async Task<IActionResult> Create()
        {
            await LoadParentCategoriesAsync();
            return View();
        }

        // POST: ProductCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ParentId,CategoryName,Slug,Description,SeoTitle,SeoDescription,Status,SortOrder")]
            ProductCategory category)
        {
            if (await _context.ProductCategories.AnyAsync(c => c.Slug == category.Slug))
            {
                ModelState.AddModelError(
                    nameof(category.Slug),
                    "Slug này đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                _context.ProductCategories.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm danh mục thành công.";
                return RedirectToAction(nameof(Index));
            }

            await LoadParentCategoriesAsync(category.ParentId);
            return View(category);
        }

        // GET: ProductCategories/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.ProductCategories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            await LoadParentCategoriesAsync(category.ParentId, category.CategoryId);
            return View(category);
        }

        // POST: ProductCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            long id,
            [Bind("CategoryId,ParentId,CategoryName,Slug,Description,SeoTitle,SeoDescription,Status,SortOrder")]
            ProductCategory category)
        {
            if (id != category.CategoryId)
            {
                return NotFound();
            }

            if (category.ParentId == category.CategoryId)
            {
                ModelState.AddModelError(
                    nameof(category.ParentId),
                    "Danh mục không thể chọn chính nó làm danh mục cha.");
            }

            bool duplicateSlug = await _context.ProductCategories
                .AnyAsync(c =>
                    c.Slug == category.Slug &&
                    c.CategoryId != category.CategoryId);

            if (duplicateSlug)
            {
                ModelState.AddModelError(
                    nameof(category.Slug),
                    "Slug này đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật danh mục thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductCategoryExists(category.CategoryId))
                    {
                        return NotFound();
                    }

                    throw;
                }
            }

            await LoadParentCategoriesAsync(
                category.ParentId,
                category.CategoryId);

            return View(category);
        }

        // GET: ProductCategories/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.ProductCategories
                .Include(c => c.Parent)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: ProductCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var category = await _context.ProductCategories
                .Include(c => c.Products)
                .Include(c => c.InverseParent)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            if (category.Products.Any())
            {
                TempData["ErrorMessage"] =
                    "Không thể xóa vì danh mục đang có sản phẩm.";

                return RedirectToAction(nameof(Index));
            }

            if (category.InverseParent.Any())
            {
                TempData["ErrorMessage"] =
                    "Không thể xóa vì danh mục đang có danh mục con.";

                return RedirectToAction(nameof(Index));
            }

            _context.ProductCategories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa danh mục thành công.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadParentCategoriesAsync(
            long? selectedId = null,
            long? excludedId = null)
        {
            var query = _context.ProductCategories
                .AsNoTracking()
                .AsQueryable();

            if (excludedId.HasValue)
            {
                query = query.Where(c => c.CategoryId != excludedId.Value);
            }

            var categories = await query
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.ParentId = new SelectList(
                categories,
                "CategoryId",
                "CategoryName",
                selectedId);
        }

        private bool ProductCategoryExists(long id)
        {
            return _context.ProductCategories
                .Any(c => c.CategoryId == id);
        }
    }
}
