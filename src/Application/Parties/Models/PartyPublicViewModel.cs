﻿using AutoMapper;
using Crpg.Application.Clans.Models;
using Crpg.Application.Common.Mappings;
using Crpg.Domain.Entities;
using Crpg.Domain.Entities.Parties;
using Crpg.Domain.Entities.Users;

namespace Crpg.Application.Parties.Models;

/// <summary>
/// View of a <see cref="Party"/> when visible. That means information like army size or position
/// are omitted.
/// </summary>
public record PartyPublicViewModel : IMapFrom<Party>
{
    public int Id { get; init; }
    public Platform Platform { get; init; }
    public string PlatformUserId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Region Region { get; init; }
    public ClanPublicViewModel? Clan { get; init; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Party, PartyPublicViewModel>()
            .ForMember(u => u.Id, opt => opt.MapFrom(u => u.Id))
            .ForMember(u => u.Platform, opt => opt.MapFrom(u => u.User!.Platform))
            .ForMember(u => u.PlatformUserId, opt => opt.MapFrom(u => u.User!.PlatformUserId))
            .ForMember(u => u.Name, opt => opt.MapFrom(u => u.User!.Name))
            .ForMember(u => u.Clan, opt => opt.MapFrom(u => u.User!.ClanMembership!.Clan));
    }
}
