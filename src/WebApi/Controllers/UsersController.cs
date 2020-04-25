using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Crpg.Application.Bans.Commands;
using Crpg.Application.Bans.Models;
using Crpg.Application.Characters;
using Crpg.Application.Characters.Commands;
using Crpg.Application.Characters.Models;
using Crpg.Application.Characters.Queries;
using Crpg.Application.Items.Commands;
using Crpg.Application.Items.Models;
using Crpg.Application.Items.Queries;
using Crpg.Application.Users.Commands;
using Crpg.Application.Users.Queries;
using Crpg.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crpg.WebApi.Controllers
{
    [ApiController]
    public class UsersController : BaseController
    {
        /// <summary>
        /// Gets current user information.
        /// </summary>
        [HttpGet("self")]
        public async Task<IActionResult> GetUser()
        {
            return Ok(await Mediator.Send(new GetUserQuery { UserId = CurrentUser.UserId }));
        }

        /// <summary>
        /// Deletes current user.
        /// </summary>
        /// <response code="204">Deleted.</response>
        /// <response code="404">User not found.</response>
        [HttpDelete("self")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> DeleteUser()
        {
            await Mediator.Send(new DeleteUserCommand { UserId = CurrentUser.UserId });
            return NoContent();
        }

        /// <summary>
        /// Gets the specified current user's character.
        /// </summary>
        /// <param name="id">Character id.</param>
        /// <response code="200">Ok.</response>
        /// <response code="404">Character not found.</response>
        [HttpGet("self/characters/{id}")]
        public async Task<ActionResult<CharacterViewModel>> GetUserCharacter([FromRoute] int id)
        {
            return Ok(await Mediator.Send(new GetUserCharacterQuery
                { CharacterId = id, UserId = CurrentUser.UserId }));
        }

        /// <summary>
        /// Gets all current user's characters.
        /// </summary>
        /// <response code="200">Ok.</response>
        [HttpGet("self/characters")]
        public async Task<ActionResult<IReadOnlyList<CharacterViewModel>>> GetUserCharactersList()
        {
            return Ok(await Mediator.Send(new GetUserCharactersListQuery { UserId = CurrentUser.UserId }));
        }

        /// <summary>
        /// Creates a new character for the current user.
        /// </summary>
        /// <param name="req">The character to create.</param>
        /// <returns>The created character.</returns>
        /// <response code="201">Created.</response>
        /// <response code="400">Bad Request.</response>
        [HttpPost("self/characters")]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        public async Task<ActionResult<CharacterViewModel>> CreateCharacter([FromBody] CreateCharacterRequest req)
        {
            var cmd = new CreateCharacterCommand { Name = req.Name, UserId = CurrentUser.UserId };
            var character = await Mediator.Send(cmd);
            return CreatedAtAction(nameof(GetUserCharacter), new { id = character.Id }, character);
        }

        /// <summary>
        /// Updates a character for the current user.
        /// </summary>
        /// <param name="id">Character id.</param>
        /// <param name="req">The entire character with the updated values.</param>
        /// <returns>The updated character.</returns>
        /// <response code="200">Updated.</response>
        /// <response code="400">Bad Request.</response>
        [HttpPut("self/characters/{id}")]
        public async Task<ActionResult<CharacterViewModel>> UpdateCharacter([FromRoute] int id,
            [FromBody] UpdateCharacterRequest req)
        {
            var cmd = new UpdateCharacterCommand
            {
                CharacterId = id,
                UserId = CurrentUser.UserId,
                Name = req.Name,
            };
            return Ok(await Mediator.Send(cmd));
        }

        /// <summary>
        /// Updates a character's items for the current user.
        /// </summary>
        /// <param name="id">Character id.</param>
        /// <param name="req">The entire character's items with the updated values.</param>
        /// <returns>The updated character.</returns>
        /// <response code="200">Updated.</response>
        /// <response code="400">Bad Request.</response>
        [HttpPut("self/characters/{id}/items")]
        public async Task<ActionResult<CharacterItemsViewModel>> UpdateCharacterItems([FromRoute] int id,
            [FromBody] UpdateCharacterItemsRequest req)
        {
            var cmd = new UpdateCharacterItemsCommand
            {
                CharacterId = id,
                UserId = CurrentUser.UserId,
                HeadItemId = req.HeadItemId,
                CapeItemId = req.CapeItemId,
                BodyItemId = req.BodyItemId,
                HandItemId = req.HandItemId,
                LegItemId = req.LegItemId,
                HorseHarnessItemId = req.HorseHarnessItemId,
                HorseItemId = req.HorseItemId,
                Weapon1ItemId = req.Weapon1ItemId,
                Weapon2ItemId = req.Weapon2ItemId,
                Weapon3ItemId = req.Weapon3ItemId,
                Weapon4ItemId = req.Weapon4ItemId,
            };
            return Ok(await Mediator.Send(cmd));
        }

        /// <summary>
        /// Retires character.
        /// </summary>
        /// <param name="id">Character id.</param>
        /// <response code="200">Retired.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="404">Character not found.</response>
        [HttpPut("self/characters/{id}/retire")]
        public async Task<ActionResult<CharacterViewModel>> UpdateCharacterItems([FromRoute] int id)
        {
            return Ok(await Mediator.Send(new RetireCharacterCommand { CharacterId = id }));
        }

        /// <summary>
        /// Deletes the specified current user's character.
        /// </summary>
        /// <param name="id">Character id.</param>
        /// <response code="204">Deleted.</response>
        /// <response code="404">Character not found.</response>
        [HttpDelete("self/characters/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> DeleteCharacter([FromRoute] int id)
        {
            await Mediator.Send(new DeleteCharacterCommand { CharacterId = id, UserId = CurrentUser.UserId });
            return NoContent();
        }

        /// <summary>
        /// Gets owned items.
        /// </summary>
        [HttpGet("self/items")]
        public async Task<ActionResult<IReadOnlyList<ItemViewModel>>> GetOwnedItems()
        {
            var query = new GetUserItemsQuery { UserId = CurrentUser.UserId };
            return Ok(await Mediator.Send(query));
        }

        /// <summary>
        /// Buys item for the current user.
        /// </summary>
        /// <param name="req">The item to buy.</param>
        /// <returns>The bought item.</returns>
        /// <response code="201">Bought.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="404">Item was not found.</response>
        [HttpPost("self/items")]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        public async Task<ActionResult<ItemViewModel>> BuyItem([FromBody] BuyItemRequest req)
        {
            var cmd = new BuyItemCommand { ItemId = req.ItemId, UserId = CurrentUser.UserId };
            var item = await Mediator.Send(cmd);
            return CreatedAtAction(nameof(ItemsController.GetItem), "Items", new { id = item.Id }, item);
        }

        /// <summary>
        /// Sells item for the current user.
        /// </summary>
        /// <param name="id">The id of the item to sell.</param>
        /// <response code="204">Sold.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="404">Item was not found.</response>
        [HttpDelete("self/items/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> SellUserItem([FromRoute] int id)
        {
            await Mediator.Send(new SellItemCommand { ItemId = id, UserId = CurrentUser.UserId });
            return NoContent();
        }

        /// <summary>
        /// Bans an user. If a ban already exists for the user, it is overriden. Use a duration of 0 to unban.
        /// </summary>
        /// <param name="id">User id to ban.</param>
        /// <param name="req">Ban info.</param>
        /// <returns>The ban object.</returns>
        /// <response code="201">Banned.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="404">User was not found.</response>
        [HttpPost("{id}/bans"), Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        public async Task<ActionResult<BanViewModel>> BanUser([FromRoute] int id, [FromBody] BanRequest req)
        {
            var ban = await Mediator.Send(new BanCommand
            {
                BannedUserId = id,
                Duration = TimeSpan.FromSeconds(req.Duration),
                Reason = req.Reason,
                BannedByUserId = CurrentUser.UserId
            });

            return StatusCode(StatusCodes.Status201Created, ban);
        }
    }
}