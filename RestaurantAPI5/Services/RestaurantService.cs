﻿using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI5.Entities;
using RestaurantAPI5.Models;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using RestaurantAPI5.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using RestaurantAPI5.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq.Expressions;
using System;

namespace RestaurantAPI5.Services
{
    // this service contains logic for:
    // * downloading all restaurants from a database,
    // * downloading specific one based on an id,
    // * creating a new one based an object RestaurantDto
    public class RestaurantService : IRestaurantService
    {
        public RestaurantService(RestaurantDbContext dbContext, IMapper mapper, ILogger<RestaurantService> logger, IAuthorizationService authorizationService, IUserContextService userContextService)
        {
            DbContext = dbContext;
            Mapper = mapper;
            _logger = logger;
            AuthorizationService = authorizationService;
            UserContextService = userContextService;
        }


        private readonly RestaurantDbContext DbContext;
        private readonly IMapper Mapper;
        private readonly ILogger _logger;

        public IAuthorizationService AuthorizationService { get; }
        public IUserContextService UserContextService { get; }

        public void Update(int id, UpdateRestaurantDto dto)
        {

            var restaurant = DbContext
                .Restaurants
                .FirstOrDefault(r => r.Id == id);

            if (restaurant is null)
                throw new NotFoundException("Restaurant not found");

            var authorizationResult = AuthorizationService.AuthorizeAsync(UserContextService.User, restaurant, new ResourceOperationRequirement(ResourceOperation.Update)).Result;

            if (!authorizationResult.Succeeded)
            {
                throw new ForbidException();
            }

            restaurant.Name = dto.Name;
            restaurant.Description = dto.Description;
            restaurant.HasDelivery = dto.HasDelivery;

            DbContext.SaveChanges();
        }

        public void Delete(int id)
        {
            _logger.LogError($"Restaurant with id: {id} DELETE action invoked");

            var restaurant = DbContext
                .Restaurants
                .FirstOrDefault(r => r.Id == id);

            if (restaurant is null)
                throw new NotFoundException("Restaurant not found");

            var authorizationResult = AuthorizationService.AuthorizeAsync(UserContextService.User, restaurant, new ResourceOperationRequirement(ResourceOperation.Delete)).Result;

            if (!authorizationResult.Succeeded)
            {
                throw new ForbidException();
            }

            DbContext.Restaurants.Remove(restaurant);
            DbContext.SaveChanges();
        }

        public RestaurantDto GetById(int id)
        {
            var restaurant = DbContext
                .Restaurants
                .Include(r => r.Address)
                .Include(r => r.Dishes)
                .FirstOrDefault(r => r.Id == id);

            if (restaurant is null)
                throw new NotFoundException("Restaurant not found");

            var result = Mapper.Map<RestaurantDto>(restaurant);
            return result;
        }


        public PagedResult<RestaurantDto> GetAll(RestaurantQuery query)
        {
            var baseQuery = DbContext
                .Restaurants
                .Include(r => r.Address)
                .Include(r => r.Dishes)
                .Where(r => query.SearchPhrase == null || (r.Name.ToLower().Contains(query.SearchPhrase.ToLower()) || r.Description.ToLower().Contains(query.SearchPhrase.ToLower())));

            if (!string.IsNullOrEmpty(query.SortBy))
            {
                var columnsSelector = new Dictionary<string, Expression<Func<Restaurant, object>>>
                {
                    { nameof(Restaurant.Name), r => r.Name },
                    { nameof(Restaurant.Description), r => r.Description },
                    { nameof(Restaurant.Category), r => r.Description }
                };

                var selectedColumn = columnsSelector[query.SortBy];

                baseQuery = query.SortDirection == SortDirection.ASC ? baseQuery.OrderBy(r => r.Name) : baseQuery.OrderByDescending(r => r.Category);
            }

            var restaurants = baseQuery
                .Skip(query.PageSize * (query.PageNumber - 1)) // pagination logic
                .Take(query.PageSize)
                .ToList();

            var totalItemsCount = baseQuery.Count();

            var restaurantsDtos = Mapper.Map<List<RestaurantDto>>(restaurants);

            var result = new PagedResult<RestaurantDto>(restaurantsDtos, totalItemsCount, query.PageSize, query.PageNumber);

            return result;
        }


        public int Create(CreateRestaurantDto dto)
        {
            var restaurant = Mapper.Map<Restaurant>(dto);
            restaurant.CreatedById = UserContextService.GetUserId;
            DbContext.Restaurants.Add(restaurant);
            DbContext.SaveChanges();

            return restaurant.Id;
        }
    }
}
