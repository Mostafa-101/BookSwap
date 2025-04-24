using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using BookSwap.Data.Contexts;

namespace BookSwap.Repos
{
    public class GenericRepo<T> where T : class

    {
        protected BookSwapDbContext _context;
        protected DbSet<T> Dbset;
        public GenericRepo(BookSwapDbContext context)
        {
            _context = context;
            Dbset = context.Set<T>();
        }
        public IEnumerable<T> getAll()
        {
            return Dbset.ToList();
        }
        public T getById(int id)
        {
            return Dbset.Find(id);
        }
        // add,delete,update functions return bool so it tells if the operation is done.
        public bool add(T item)
        {
            try
            {
                _context.Add(item);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            var added = _context.SaveChanges();//return number of rows affected
            return added > 0;
        }

        public bool update(T item)
        { 
            try
            {
                _context.Attach(item);
                _context.Entry(item).State = EntityState.Modified;
            }
            catch (Exception ex)
            {
                throw new Exception(message: ex.Message);
            }
            var updated = _context.SaveChanges();
            return updated > 0;
        }
        public EntityEntry CheckState(T item)
            => _context.Entry(item);



        public bool remove(int id)
        {
            int deleted;
            var item = Dbset.Find(id);
            if (item != null)
            {
                try
                {
                    _context.Remove(item);

                    deleted = _context.SaveChanges();
                }
                catch (Exception ex)
                {
                    throw new Exception("can't remove");
                }
                return deleted > 0;
            }
            return false;

        }
        /*a get all function that let the filteration to accure in the server , to avoid bringing all the data in the memory,
         it can be used as : getAllFilter(//the filter: =>e.Age<30 , //if i need to use include: e=>e.Include(e.Department)),
        or i just can use it empty as getAllFilter() and it will bring all the rows
         */
        public async Task<IEnumerable<T>> getAllFilterAsync(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IQueryable<T>> include = null)
        {
            IQueryable<T> query = Dbset;
            if (filter != null)
            {
                query = query.Where(filter);
            }
            if (include != null)
            {
                query = include(query);
            }
            return await query.ToListAsync();
        }

    }
}

