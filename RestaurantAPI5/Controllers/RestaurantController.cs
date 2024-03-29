﻿using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RestaurantAPI5.Entities;
using RestaurantAPI5.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI5.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace RestaurantAPI5.Controllers
{
    //mapping requests with specific paths:
    [Route("api/restaurant")]
    [ApiController]
    [Authorize]
    public class RestaurantController : ControllerBase // ControllerBase allows access to request and answer context
    {
        public RestaurantController(IRestaurantService restaurantService)
        {
            RestaurantService = restaurantService;
        }
        public IRestaurantService RestaurantService { get; }

        // Update
        [HttpPut("{id}")]
        public ActionResult UpdateRestaurant([FromBody] UpdateRestaurantDto dto, [FromRoute]int id)
        {
            RestaurantService.Update(id, dto);
            return Ok();
        }


        [HttpDelete("{id}")]
        public ActionResult Delete([FromRoute] int id)
        {
            RestaurantService.Delete(id);
            return NoContent();
        }


        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]

        public ActionResult CreateRestaurant([FromBody] CreateRestaurantDto dto)
        {
            var id = RestaurantService.Create(dto);
            return Created($"/api/restaurant/{id}", null);
        }



        [HttpGet]
        [Authorize(Policy = "CreatedAtleast2Restaurants")]
        public ActionResult<IEnumerable<RestaurantDto>> GetAll([FromQuery]RestaurantQuery query)
        {
            var restaurantsDtos = RestaurantService.GetAll(query);

            return Ok(restaurantsDtos);
        }



        [HttpGet("{id}")] // it's a good practice to assign an action by Http attributes
        [AllowAnonymous]
        public ActionResult<RestaurantDto> Get([FromRoute] int id)
        {
            var restaurant = RestaurantService.GetById(id);
            return Ok(restaurant);
        }
    }
}