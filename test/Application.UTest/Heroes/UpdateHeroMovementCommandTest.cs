﻿using System.Threading;
using System.Threading.Tasks;
using Crpg.Application.Common.Results;
using Crpg.Application.Common.Services;
using Crpg.Application.Heroes.Commands;
using Crpg.Domain.Entities.Heroes;
using Crpg.Domain.Entities.Settlements;
using Crpg.Domain.Entities.Users;
using Moq;
using NetTopologySuite.Geometries;
using NUnit.Framework;

namespace Crpg.Application.UTest.Heroes
{
    public class UpdateHeroStatusCommandTest : TestBase
    {
        [Test]
        public async Task ShouldReturnErrorIfNotFound()
        {
            var handler = new UpdateHeroStatusCommand.Handler(ActDb, Mapper, Mock.Of<IStrategusMap>());
            var res = await handler.Handle(new UpdateHeroStatusCommand
            {
                HeroId = 1,
                Waypoints = MultiPoint.Empty,
            }, CancellationToken.None);

            Assert.IsNotNull(res.Errors);
            Assert.AreEqual(ErrorCode.HeroNotFound, res.Errors![0].Code);
        }

        [Test]
        public async Task ShouldReturnErrorUserNotRegisteredToStrategus()
        {
            var user = new User();
            ArrangeDb.Users.Add(user);
            await ArrangeDb.SaveChangesAsync();

            var handler = new UpdateHeroStatusCommand.Handler(ActDb, Mapper, Mock.Of<IStrategusMap>());
            var res = await handler.Handle(new UpdateHeroStatusCommand
            {
                HeroId = user.Id,
                Waypoints = MultiPoint.Empty,
            }, CancellationToken.None);

            Assert.IsNotNull(res.Errors);
            Assert.AreEqual(ErrorCode.HeroNotFound, res.Errors![0].Code);
        }

        [Test]
        public async Task ShouldReturnErrorIfUserIsInBattle()
        {
            var user = new User
            {
                Hero = new Hero
                {
                    Status = HeroStatus.InBattle,
                },
            };
            ArrangeDb.Users.Add(user);
            await ArrangeDb.SaveChangesAsync();

            var handler = new UpdateHeroStatusCommand.Handler(ActDb, Mapper, Mock.Of<IStrategusMap>());
            var res = await handler.Handle(new UpdateHeroStatusCommand
            {
                HeroId = user.Id,
                Waypoints = new MultiPoint(new[]
                {
                    new Point(4, 5),
                    new Point(6, 7),
                }),
            }, CancellationToken.None);

            Assert.IsNotNull(res.Errors);
            Assert.AreEqual(ErrorCode.HeroInBattle, res.Errors![0].Code);
        }

        [Test]
        public async Task ShouldClearMovementIfStatusIdle()
        {
            var user = new User
            {
                Hero = new Hero
                {
                    Status = HeroStatus.MovingToSettlement,
                    // Doesn't make sense that the 3 properties are set but it's just for the test.
                    Waypoints = new MultiPoint(new[] { new Point(5, 3) }),
                    TargetedHero = new Hero { User = new User() },
                    TargetedSettlement = new Settlement(),
                },
            };
            ArrangeDb.Users.Add(user);
            await ArrangeDb.SaveChangesAsync();

            var handler = new UpdateHeroStatusCommand.Handler(ActDb, Mapper, Mock.Of<IStrategusMap>());
            var res = await handler.Handle(new UpdateHeroStatusCommand
            {
                HeroId = user.Id,
                Status = HeroStatus.Idle,
            }, CancellationToken.None);

            var hero = res.Data!;
            Assert.IsNotNull(hero);
            Assert.AreEqual(user.Id, hero.Id);
            Assert.AreEqual(HeroStatus.Idle, hero.Status);
            Assert.AreEqual(0, hero.Waypoints.Count);
            Assert.IsNull(hero.TargetedHero);
            Assert.IsNull(hero.TargetedSettlement);
        }

        [Test]
        public async Task ShouldUpdateHeroMovementIfStatusMovingToPoint()
        {
            var user = new User
            {
                Hero = new Hero
                {
                    Status = HeroStatus.Idle,
                    Waypoints = new MultiPoint(new[] { new Point(3, 4) }),
                },
            };
            ArrangeDb.Users.Add(user);
            await ArrangeDb.SaveChangesAsync();

            var handler = new UpdateHeroStatusCommand.Handler(ActDb, Mapper, Mock.Of<IStrategusMap>());
            var res = await handler.Handle(new UpdateHeroStatusCommand
            {
                HeroId = user.Id,
                Status = HeroStatus.MovingToPoint,
                Waypoints = new MultiPoint(new[]
                {
                    new Point(4, 5),
                    new Point(6, 7),
                }),
            }, CancellationToken.None);

            var hero = res.Data!;
            Assert.IsNotNull(hero);
            Assert.AreEqual(user.Id, hero.Id);
            Assert.AreEqual(HeroStatus.MovingToPoint, hero.Status);
            Assert.AreEqual(2, hero.Waypoints.Count);
            Assert.AreEqual(new Point(4, 5), hero.Waypoints[0]);
            Assert.AreEqual(new Point(6, 7), hero.Waypoints[1]);
        }

        [TestCase(HeroStatus.FollowingHero)]
        [TestCase(HeroStatus.MovingToAttackHero)]
        public async Task ShouldReturnErrorIfTargetingNotExistingUser(HeroStatus status)
        {
            var user = new User
            {
                Hero = new Hero
                {
                    Status = HeroStatus.MovingToAttackSettlement,
                    TargetedSettlement = new Settlement(),
                },
            };
            ArrangeDb.Users.AddRange(user);
            await ArrangeDb.SaveChangesAsync();

            var handler = new UpdateHeroStatusCommand.Handler(ActDb, Mapper, Mock.Of<IStrategusMap>());
            var res = await handler.Handle(new UpdateHeroStatusCommand
            {
                HeroId = user.Id,
                Status = status,
                TargetedHeroId = 10,
            }, CancellationToken.None);

            Assert.IsNotNull(res.Errors);
            Assert.AreEqual(ErrorCode.UserNotFound, res.Errors![0].Code);
        }

        [TestCase(HeroStatus.FollowingHero)]
        [TestCase(HeroStatus.MovingToAttackHero)]
        public async Task ShouldReturnErrorIfTargetingNotVisibleHero(HeroStatus status)
        {
            var user = new User
            {
                Hero = new Hero
                {
                    Position = new Point(0, 0),
                    Status = HeroStatus.MovingToSettlement,
                    TargetedSettlement = new Settlement(),
                },
            };
            var targetUser = new User
            {
                Hero = new Hero
                {
                    Position = new Point(10, 10),
                },
            };
            ArrangeDb.Users.AddRange(user, targetUser);
            await ArrangeDb.SaveChangesAsync();

            var strategusMapMock = new Mock<IStrategusMap>();
            strategusMapMock.Setup(m => m.ViewDistance).Returns(0);

            var handler = new UpdateHeroStatusCommand.Handler(ActDb, Mapper, strategusMapMock.Object);
            var res = await handler.Handle(new UpdateHeroStatusCommand
            {
                HeroId = user.Id,
                Status = status,
                TargetedHeroId = targetUser.Id,
            }, CancellationToken.None);

            Assert.IsNotNull(res.Errors);
            Assert.AreEqual(ErrorCode.HeroNotInSight, res.Errors![0].Code);
        }

        [TestCase(HeroStatus.FollowingHero)]
        [TestCase(HeroStatus.MovingToAttackHero)]
        public async Task ShouldUpdateHeroMovementIfTargetingUser(HeroStatus status)
        {
            var user = new User
            {
                Hero = new Hero
                {
                    Position = new Point(0, 0),
                    Status = HeroStatus.MovingToSettlement,
                    TargetedSettlement = new Settlement(),
                },
            };
            var targetUser = new User
            {
                Hero = new Hero
                {
                    Position = new Point(10, 10),
                },
            };
            ArrangeDb.Users.AddRange(user, targetUser);
            await ArrangeDb.SaveChangesAsync();

            var strategusMapMock = new Mock<IStrategusMap>();
            strategusMapMock.Setup(m => m.ViewDistance).Returns(500);

            var handler = new UpdateHeroStatusCommand.Handler(ActDb, Mapper, strategusMapMock.Object);
            var res = await handler.Handle(new UpdateHeroStatusCommand
            {
                HeroId = user.Id,
                Status = status,
                TargetedHeroId = targetUser.Id,
            }, CancellationToken.None);

            var hero = res.Data!;
            Assert.IsNotNull(hero);
            Assert.AreEqual(user.Id, hero.Id);
            Assert.AreEqual(status, hero.Status);
            Assert.AreEqual(targetUser.Id, hero.TargetedHero!.Id);
            Assert.IsNull(hero.TargetedSettlement);
        }

        [Test]
        public async Task ShouldSwitchFromAttackToFollowUser()
        {
            User targetUser = new()
            {
                Hero = new Hero { Position = new Point(10, 10) },
            };
            var user = new User
            {
                Hero = new Hero
                {
                    Position = new Point(0, 0),
                    Status = HeroStatus.MovingToAttackHero,
                    TargetedHero = targetUser.Hero,
                },
            };
            ArrangeDb.Users.AddRange(user, targetUser);
            await ArrangeDb.SaveChangesAsync();

            var strategusMapMock = new Mock<IStrategusMap>();
            strategusMapMock.Setup(m => m.ViewDistance).Returns(500);

            var handler = new UpdateHeroStatusCommand.Handler(ActDb, Mapper, strategusMapMock.Object);
            var res = await handler.Handle(new UpdateHeroStatusCommand
            {
                HeroId = user.Id,
                Status = HeroStatus.FollowingHero,
                TargetedHeroId = targetUser.Id,
            }, CancellationToken.None);

            var hero = res.Data!;
            Assert.IsNotNull(hero);
            Assert.AreEqual(user.Id, hero.Id);
            Assert.AreEqual(HeroStatus.FollowingHero, hero.Status);
            Assert.AreEqual(targetUser.Id, hero.TargetedHero!.Id);
        }

        [TestCase(HeroStatus.MovingToSettlement)]
        [TestCase(HeroStatus.MovingToAttackSettlement)]
        public async Task ShouldReturnErrorIfTargetingNotExistingSettlement(HeroStatus status)
        {
            var user = new User
            {
                Hero = new Hero
                {
                    Status = HeroStatus.FollowingHero,
                    TargetedHero = new Hero { User = new User() },
                },
            };
            ArrangeDb.Users.Add(user);
            await ArrangeDb.SaveChangesAsync();

            var handler = new UpdateHeroStatusCommand.Handler(ActDb, Mapper, Mock.Of<IStrategusMap>());
            var res = await handler.Handle(new UpdateHeroStatusCommand
            {
                HeroId = user.Id,
                Status = status,
                TargetedSettlementId = 10,
            }, CancellationToken.None);

            Assert.IsNotNull(res.Errors);
            Assert.AreEqual(ErrorCode.SettlementNotFound, res.Errors![0].Code);
        }

        [TestCase(HeroStatus.MovingToSettlement)]
        [TestCase(HeroStatus.MovingToAttackSettlement)]
        public async Task ShouldUpdateHeroMovementIfTargetingSettlement(HeroStatus status)
        {
            var user = new User
            {
                Hero = new Hero
                {
                    Status = HeroStatus.MovingToAttackHero,
                    TargetedHero = new Hero { User = new User() },
                },
            };
            var targetSettlement = new Settlement();
            ArrangeDb.Users.Add(user);
            ArrangeDb.Settlements.Add(targetSettlement);
            await ArrangeDb.SaveChangesAsync();

            var handler = new UpdateHeroStatusCommand.Handler(ActDb, Mapper, Mock.Of<IStrategusMap>());
            var res = await handler.Handle(new UpdateHeroStatusCommand
            {
                HeroId = user.Id,
                Status = status,
                TargetedSettlementId = targetSettlement.Id,
            }, CancellationToken.None);

            var hero = res.Data!;
            Assert.IsNotNull(hero);
            Assert.AreEqual(user.Id, hero.Id);
            Assert.AreEqual(status, hero.Status);
            Assert.IsNull(hero.TargetedHero);
            Assert.AreEqual(targetSettlement.Id, hero.TargetedSettlement!.Id);
        }

        [Test]
        public async Task ShouldSwitchFromMoveToAttackSettlement()
        {
            var targetSettlement = new Settlement();
            var user = new User
            {
                Hero = new Hero
                {
                    Position = new Point(0, 0),
                    Status = HeroStatus.MovingToSettlement,
                    TargetedSettlement = targetSettlement,
                },
            };
            ArrangeDb.Users.AddRange(user);
            await ArrangeDb.SaveChangesAsync();

            var handler = new UpdateHeroStatusCommand.Handler(ActDb, Mapper, Mock.Of<IStrategusMap>());
            var res = await handler.Handle(new UpdateHeroStatusCommand
            {
                HeroId = user.Id,
                Status = HeroStatus.MovingToAttackSettlement,
                TargetedSettlementId = targetSettlement.Id,
            }, CancellationToken.None);

            var hero = res.Data!;
            Assert.IsNotNull(hero);
            Assert.AreEqual(user.Id, hero.Id);
            Assert.AreEqual(HeroStatus.MovingToAttackSettlement, hero.Status);
            Assert.AreEqual(targetSettlement.Id, hero.TargetedSettlement!.Id);
        }

        [Test]
        public async Task ShouldReturnErrorIfTryingToStopRecruitingWhenNotInASettlement()
        {
            var user = new User
            {
                Hero = new Hero { Status = HeroStatus.Idle },
            };
            ArrangeDb.Users.Add(user);
            await ArrangeDb.SaveChangesAsync();

            var handler = new UpdateHeroStatusCommand.Handler(ActDb, Mapper, Mock.Of<IStrategusMap>());
            var res = await handler.Handle(new UpdateHeroStatusCommand
            {
                HeroId = user.Id,
                Status = HeroStatus.IdleInSettlement,
            }, CancellationToken.None);

            Assert.IsNotNull(res.Errors);
            Assert.AreEqual(ErrorCode.HeroNotInASettlement, res.Errors![0].Code);
        }

        [Test]
        public async Task ShouldSwitchFromRecruitingInSettlementToIdleInSettlement()
        {
            var user = new User
            {
                Hero = new Hero
                {
                    Status = HeroStatus.RecruitingInSettlement,
                    TargetedSettlement = new Settlement(),
                },
            };
            ArrangeDb.Users.Add(user);
            await ArrangeDb.SaveChangesAsync();

            var handler = new UpdateHeroStatusCommand.Handler(ActDb, Mapper, Mock.Of<IStrategusMap>());
            var res = await handler.Handle(new UpdateHeroStatusCommand
            {
                HeroId = user.Id,
                Status = HeroStatus.IdleInSettlement,
            }, CancellationToken.None);

            Assert.IsNull(res.Errors);
            Assert.AreEqual(HeroStatus.IdleInSettlement, res.Data!.Status);
            Assert.IsNotNull(res.Data!.TargetedSettlement);
        }

        [Test]
        public async Task ShouldReturnErrorIfTryingToRecruitWhenNotInASettlement()
        {
            var user = new User
            {
                Hero = new Hero { Status = HeroStatus.Idle },
            };
            ArrangeDb.Users.Add(user);
            await ArrangeDb.SaveChangesAsync();

            var handler = new UpdateHeroStatusCommand.Handler(ActDb, Mapper, Mock.Of<IStrategusMap>());
            var res = await handler.Handle(new UpdateHeroStatusCommand
            {
                HeroId = user.Id,
                Status = HeroStatus.RecruitingInSettlement,
            }, CancellationToken.None);

            Assert.IsNotNull(res.Errors);
            Assert.AreEqual(ErrorCode.HeroNotInASettlement, res.Errors![0].Code);
        }

        [Test]
        public async Task ShouldSwitchFromIdleInSettlementToRecruitingInSettlement()
        {
            var user = new User
            {
                Hero = new Hero
                {
                    Status = HeroStatus.IdleInSettlement,
                    TargetedSettlement = new Settlement(),
                },
            };
            ArrangeDb.Users.Add(user);
            await ArrangeDb.SaveChangesAsync();

            var handler = new UpdateHeroStatusCommand.Handler(ActDb, Mapper, Mock.Of<IStrategusMap>());
            var res = await handler.Handle(new UpdateHeroStatusCommand
            {
                HeroId = user.Id,
                Status = HeroStatus.RecruitingInSettlement,
            }, CancellationToken.None);

            Assert.IsNull(res.Errors);
            Assert.AreEqual(HeroStatus.RecruitingInSettlement, res.Data!.Status);
            Assert.IsNotNull(res.Data!.TargetedSettlement);
        }
    }
}
