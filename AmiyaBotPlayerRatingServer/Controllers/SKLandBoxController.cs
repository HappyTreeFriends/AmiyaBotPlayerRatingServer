﻿using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AmiyaBotPlayerRatingServer.Controllers.Policy;
using AmiyaBotPlayerRatingServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace AmiyaBotPlayerRatingServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SKLandBoxController : ControllerBase
    {
        private readonly PlayerRatingDatabaseContext _context;

        public SKLandBoxController(PlayerRatingDatabaseContext context)
        {
            _context = context;
        }

        [HttpGet("GetBoxByCredential/{credentialId}")]
        [Authorize(Policy = CredentialOwnerPolicy.Name)]
        public async Task<IActionResult> GetBoxByCredential(string credentialId)
        {
            // 获取当前用户ID
            var userId = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // 从数据库中找到对应的CharacterBox
            var characterBox = await _context.SKLandCharacterBoxes
                .Include(box => box.Credential)  // Include the related SKLandCredential
                .AsNoTracking()
                .FirstOrDefaultAsync(box => box.CredentialId == credentialId);

            if (characterBox == null)
            {
                return NotFound("Character box not found.");
            }

            return Ok(new
            {
                Id = characterBox.Id,
                CredentialId = characterBox.CredentialId,
                CharacterBoxJson = characterBox.CharacterBoxJson
            });
        }
    }
}