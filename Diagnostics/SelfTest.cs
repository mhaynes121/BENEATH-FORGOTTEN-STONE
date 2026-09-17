using System.Text.Json;
using BENEATH_FORGOTTEN_STONE.Core;
using BENEATH_FORGOTTEN_STONE.Core.Screens;
using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.AI;
using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Proficiency;
using BENEATH_FORGOTTEN_STONE.Entities.Projectiles;
using BENEATH_FORGOTTEN_STONE.Entities.Skills;
using BENEATH_FORGOTTEN_STONE.Entities.Sounds;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;
using BENEATH_FORGOTTEN_STONE.Persistence;

namespace BENEATH_FORGOTTEN_STONE.Diagnostics;

/// <summary>
/// Headless smoke test for game LOGIC, run via `BENEATH_FORGOTTEN_STONE.exe --selftest` (see
/// Program.cs). Exists because almost every screen in this game reads input via
/// Console.ReadKey directly, which throws/returns immediately when stdin isn't a real
/// console -- there's no way to drive the interactive UI from an automated harness yet.
/// This instead calls the underlying logic (Monster.CreateRandom, LootGenerator,
/// Actor.PhysicalAttack, ItemEffectApplier, EffectProcessor, SpellCaster, and two
/// InventoryScreen methods bumped to internal specifically so they're reachable here)
/// directly, bypassing every screen. If the interactive flows themselves ever need
/// covering too, the natural next step is an input-provider abstraction the screens
/// read through, which this file's checks could then be extended to drive.
///
/// Not a unit test project on purpose (this project deliberately has none) -- just a
/// single self-contained diagnostic entry point in the same assembly, so it needs no
/// project reference or test framework to run.
/// </summary>
public static class SelfTest
{
    private static int passed;
    private static int failed;
    private static int skipped;

    public static int Run()
    {
        Console.WriteLine("=== BENEATH_FORGOTTEN_STONE self-test ===");
        Console.WriteLine();

        SchedulerNeverStarvesAFrozenLowSpeedActor();
        SchedulerDoesNotInflateEnergyOnFreeRepolls();
        CreatureCapabilityDefaults();
        LootGeneratorGating();
        BlessedWeaponVsUndead();
        ProtectionMitigation();
        OnHitInstantDamage();
        OnHitDamageOverTime();
        OnHitStun();
        OnHitCorrode();
        ThornsReflection();
        EffectProcessorResistance();
        EffectProcessorRegeneration();
        IdentifySpell();
        PerInstanceIdentification();
        CursedItemBlocksUnequip();
        UnidentifiedItemHidesDetails();
        SwordAndMaceShareCategoryButNotWithShieldOrLegArmor();
        RingsAndArmorSlotsOnlyCompareWithinTheirOwnType();
        HybridSpearClassifiesByItsActualEquippedSlot();
        ExpectedAndMaximumProcValuesReflectMagnitudeDurationAndChance();
        RegenerationUsesTheConfiguredHorizonUnlessADurationIsShorter();
        NegativeModifiersAndCurseFlatPenaltySubtractFromQuality();
        UnidentifiedItemRatingIsIncompleteWithOnlyBaseValueKnown();
        BothCompleteRatingsDeclareADefinitiveWinnerOrATie();
        ItemComparerNeverDeclaresADefinitiveWinnerWhenEitherRatingIsIncomplete();
        ProcDependentFlagReflectsWhetherMaximumProcWouldChangeTheWinner();
        FindsAWeaponBehindAShieldAndNeverSelectsTheShieldItself();
        DualWieldSelectsTheWeakerOfTwoIdentifiedEquippedWeapons();
        CursedOrUnidentifiedEquippedItemsAreNeverSelectedAsAReplacementTarget();
        CanReplaceInContextEnforcesFullEquipLegality();
        UnidentifiedPickupNeverOffersToEquipButHintsWhenACounterpartExists();
        UnidentifiedPickupWithNoComparableCounterpartShowsNoExtraHint();
        UnidentifiedPickupNeverOffersReplacementEvenWithAFullLoadout();
        GenuineIdentifiedUpgradeIsFoundBehindAFullLoadout();
        TiedOrWorsePickupsProduceNoUpgradeOffer();
        GetAllNeverTriggersPickupUpgradeReplacement();
        ExamineShowsTheLightweightComparisonOnlyWhenApplicable();
        MultipleUnidentifiedEquippedCounterpartsSuppressTheLightweightLine();
        OwnedItemLabelAndTraderPriceLineFormatCorrectly();
        FormatDeltaSentenceWordingCoversAllFourResultShapes();
        DeathAndPickupMessagesNeverLeakAnUnidentifiedItemsName();
        StartingGearIsAlwaysIdentified();
        StartingGearLauncherAlwaysComesWithMatchingAmmo();
        LuckRollsWithinEachRacesOwnRangeAcrossEveryClass();
        HalflingHasTheHighestAverageLuckAndDwarfTheLowest();
        EveryClassHasAnExplicitZeroLuckModifier();
        LuckSurvivesASaveLoadRoundTrip();
        OldSaveDataMissingLuckMigratesToAFixedRacialMidpoint();
        LookNamesTheTarget();
        LookOnABossShowsCreatureTypeAlongsideItsIdentity();
        RenderIsSkippedForOffScreenMonsters();
        BossHasScaledStatsAndGuaranteedEquipment();
        BossDisplayNameIsIndependentOfCreatureType();
        DungeonGenerationCanProduceABossRoom();
        DungeonGeneratesOnlyRoomsAndStrictOneWideHalls();
        DungeonFloorIsFullyConnectedThroughDoors();
        TraderIsImmuneToTargeting();
        TraderNeverActsInTheScheduler();
        TraderSpawnRespectsBossRoomAndStairsExclusion();
        TraderFrequencyGuaranteeNeverMissesThreeFloors();
        TraderInventoryCountAndGoldValueArePopulated();
        CharismaAdjustsTradePrices();
        TraderBuySellTransactions();
        TraderIdentificationService();
        TraderRestoreDataRoundTrip();
        TraderScreenColorsReflectUsabilityAndAffordability();
        ProjectileStopsAtWall();
        FiredProjectileHittingAnInvisibleOccupantNeverRevealsItsName();
        ProjectilePenetrationHitsMultipleDistinctTargetsOnce();
        ProjectileFriendlyFireRulesPlayerSource();
        ProjectileFriendlyFireRulesMonsterSource();
        ProjectileReusesSameStatusEffectPipelineAsMelee();
        ProjectileAppliesBlessedUndeadBonusJustLikeMelee();
        SpellIsProjectileClassification();
        SpellCastOnTargetsResolvedHookFiresExactlyOnce();
        FiringDoesNotAutoIdentifyTheAmmo();
        ThrownItemsDefaultToDropsAtImpactPointUnlessOverridden();
        DeathCausePhraseReadsAsAGrammaticalSentence();
        ThrowableCategoryClassGating();
        ItemUsabilityAccountsForClassEquipSpellAndThrowRestrictions();
        MageCanCastIdentifyAndItAlwaysSucceeds();
        ThiefIdentifySkillChanceScalesWithKnowledgeStat();
        ThiefIdentifySkillCanSucceedAndFail();
        NoDuplicateItemNamesAcrossTheFullCatalogExceptDeliberateContainerVariants();
        TurnCountStartsAtZero();
        ClassRegenDivisorsMatchDesignTable();
        HpRegenRateMatchesUserSpecifiedTargets();
        ManaRegenRateMatchesUserSpecifiedTargets();
        CombatRegenRateIsOneTenthOfNormalRate();
        FractionalRegenAccumulatesToWholePointsOnSchedule();
        HigherConstitutionRegeneratesFasterOverTime();
        PlayerInCombatDetectsOwnAttacksAndAdjacentMonsterAttacks();
        PlayerInCombatIgnoresAStaleLastAttackTurnFromAPreviousLevel();
        TraderInventoryCapRejectsFurtherSellsWithVarietyMessage();
        TraderColumnLayoutRespectsSixteenPerColumnRule();
        MenuPromptColumnLayoutRespectsSixteenPerColumnRule();
        PickUpAllPicksUpEveryItemOnTheTileInOneAction();
        ShieldSizeRestrictionsMatchDesign();
        ThiefWeaponSizeRestrictionsMatchDesign();
        FloorTypeDefaultsToNormal();
        FloorTypeVisualDefinitionsMatchSpec();
        FloorTypeElementalMultipliersMatchSpec();
        FloorDamageCalculatorAppliesTheRightMultiplier();
        EnvironmentalDamageOnlyFromFireAndLava();
        EnvironmentalDamageScalesWithDungeonLevelAndLavaExceedsFire();
        DungeonRoomsNeverMixTwoSpecialFloorTypes();
        DungeonSpecialFloorRoomsMeetMinimumCoverage();
        DungeonHallwaysAndOtherRoomsStayNormal();
        TraderNeverSpawnsAdjacentToADoor();
        SkeletonKeysStackByCharges();
        SkeletonKeyDecrementsChargesInsteadOfAlwaysRemoving();
        SkeletonKeySpawnsAreIndependentInstancesNotASharedReference();
        ItemStackingCoversEveryRequestedCategory();
        PotionStackingCapsAtFiveThenStartsANewStack();
        ConsumingOneUnitFromAStackDecrementsInsteadOfRemovingTheWholeStack();
        TraderStockMergesStackableItemsInsteadOfListingDuplicates();
        StackableItemGoldValueScalesWithCurrentChargesNotFrozenAtSpawnTime();
        TraderStackSuffixShowsQuantityOnlyWhenGreaterThanOne();
        ElementalOppositionTableMatchesDesignSpec();
        FloorAttunedArchetypesHaveCorrectPreferredFloorAffinityAndOpposingElement();
        FloorAttunementAttritionOnlyAppliesOffPreferredFloorAndUsesMaxHp();
        FloorAttunementAttritionMessageUsesSeverityWordNotRawNumber();
        FloorAttunementAttritionAlwaysAtLeastOneDamage();
        FloorAttunementNeverAppliesToAnOrdinaryMonster();
        FloorAttunedMonsterResistanceSwingsWithTerrain();
        TerrainPathfinderPrefersLongerPreferredFloorRouteOverShorterNormalRoute();
        TerrainPathfinderFindsNearestReachablePreferredTile();
        FloorAttunedMonstersOnlySpawnOnTheirOwnPreferredFloorType();
        FloorAttunedMonsterNeverSelectedWhenItsFloorTypeIsAbsentFromTheLevel();
        DeathFromFloorAttunementAttritionUsesNormalDeathPipeline();
        GraveyardKeepsOnlyTheTenDeepestDeaths();
        GraveyardTiesBrokenByMostRecentDeath();
        ResistanceBaseValuesMatchRaceClassStatTable();
        ResistanceConstitutionModifierTableMatchesSpec();
        ResistanceWisdomModifierTableMatchesSpec();
        ResistanceEquipmentAddsToEffectiveTotal();
        ResistanceCursedEquipmentSubtractsFromEffectiveTotal();
        ResistanceMultipleItemsStackAdditively();
        ResistanceClampsToConfiguredRange();
        ResistanceReducesIncomingDamage();
        NegativeResistanceIncreasesIncomingDamage();
        ResistanceReducesStatusApplicationChance();
        ResistanceReducesStatusDurationAtHalfStrength();
        MagicResistanceAffectsArcaneNotElementalSpells();
        FloorDamageCalculatorComposesFloorMultiplierWithResistance();
        TileAttunedMonsterNeverDoubleAppliesResistance();
        ResistanceBreakdownSumsToEffectiveValue();
        InventoryResistLineFormatMatchesSpec();
        SoundCooldownBlocksFurtherSoundsUntilItExpires();
        SoundChanceIncreasesWithLongerSilence();
        SoundChanceNeverGuaranteedEvenAfterExtremeSilence();
        WisdomModestlyShiftsHearingChanceWithoutExtremes();
        SoundDistanceRespectsMaxRange();
        CloserSoundSourceHasHigherChanceThanFarther();
        CreatureAmbientPoolReflectsLivingMonsters();
        DuplicateMonsterSoundTypesAppearOnlyOnce();
        SilentMonsterNeverContributesAmbientSound();
        BossSoundStopsAfterBossDeath();
        EnvironmentSourceOnlyBecomesCandidateWithinRange();
        RecentlyUsedSoundKeysAreAvoidedWhenAlternativesExist();
        SoundSystemAdvancesSilenceTimerExactlyOncePerCall();
        InventoryResistLineRefreshesOnEquipAndUnequip();
        PlayerKillingAMonsterAwardsNormalXpAndGold();
        EnvironmentalKillAwardsNoXpOrGold();
        PlayerAppliedPoisonKillCreditsThePlayerEvenAfterTheyStoppedAttacking();
        EnvironmentalDamageNeverLeavesAStalePlayerOwnedCreditBehind();
        OneMonsterKillingAnotherAwardsThePlayerNothing();
        LootStillDropsRegardlessOfWhoGetsKillCredit();
        ScrollsSpellbooksAndAmmunitionDefaultToFlammable();
        WeaponsArmorAndPotionsDefaultToNonFlammable();
        FlammableItemIsDestroyedLandingOnFire();
        FlammableItemIsDestroyedLandingOnLava();
        NonFlammableItemNeverDestroyedByFireOrLava();
        FireproofItemSurvivesFireButNotLava();
        LavaProofItemSurvivesLavaButNotPlainFire();
        MessageLogCapsHistoryAtTwentyEntriesDroppingOldestFirst();
        MessageLogGetRecentReturnsOnlyTheMostRecentEntriesInOrder();
        DescendingWhileStandingOnStairsDownAdvancesAFloorAndConsumesATurn();
        AscendingWhileStandingOnStairsUpReturnsAFloorAndConsumesATurn();
        StandingOnTheWrongStairwayShowsAMessageAndConsumesNoTurn();
        UsingStairsAwayFromAnyStairwayShowsAMessageAndConsumesNoTurn();
        AscendingFromTheTopmostFloorFailsAndConsumesNoTurn();
        KnownAilmentExpirationUsesAtmosphericWordingNotGenericFades();
        UnknownEffectExpirationFallsBackToGenericFadesWording();
        VisibleMonsterExpirationShowsAMessage();
        OffScreenMonsterExpirationShowsNoMessage();
        NoExpirationMessageWhenTheExpiringDamageJustKilledTheActor();
        CenterPadsTextRoughlyEquallyOnBothSides();
        CenterReturnsTextUnchangedWhenItAlreadyFillsOrExceedsTheWidth();
        MainMenuTitleBlockContainsTheGameTitleAndSubtitle();
        StoryScreenBuildBlocksPreservesParagraphsAsAtomicUnitsAndWrapsToWidth();
        StoryScreenPaginateNeverSplitsABlockAcrossAPageBreak();
        StoryScreenPaginateSeparatesBlocksOnTheSamePageWithExactlyOneBlankLine();
        StoryScreenPaginateAlwaysReturnsAtLeastOnePage();
        OpeningStoryAtFixedConsoleDimensionsFitsAndNeverSplitsAParagraph();
        OpeningStoryTextContainsEveryKeyBeat();
        DarkRoomTileHiddenWithoutIllumination();
        DarkRoomTileVisibleOnceIlluminated();
        NonDarkRoomTileNeedsOnlyLineOfSight();
        BlindObserverCannotDiscoverAnUnseenNonDarkTileAcrossADarkRoom();
        BlindObserverStillSeesAPreviouslyExploredTileAsRemembered();
        BlindObserverSeesAnyCurrentlyIlluminatedTileRegardlessOfPriorExploration();
        IlluminationRespectsWalls();
        CollectActiveLightSourcesFindsLitItemsAndLightEffects();
        UnlitItemIsNotACollectedLightSource();
        CandleTorchLanternMatchDesignedBalanceValues();
        LightingSystemProcessTurnDecrementsDurationAndDestroysExhaustedConsumables();
        LightingSystemProcessTurnPreservesAnExhaustedLantern();
        RandomExtinguishNeverConsumesRemainingDuration();
        ExtinguishForWaterPutsOutLitWaterVulnerableItemsOnlyOnce();
        LightingAnItemViaInventoryTogglesIsLit();
        RelightingAnExtinguishedItemResumesFromRemainingDurationNotFull();
        LightingAnAlreadyExhaustedItemFails();
        PlayerInDarknessReflectsTileDarkAndIlluminationState();
        AttackFromDarknessHidesAttackerIdentityInBothHitAndMissMessages();
        PlayerAttackingSomethingUnseenAlsoHidesItsIdentity();
        BumpAttackingAnInvisibleOccupantNeverRevealsItsName();
        VisibleAttackerMessagesAreUnaffectedByTheNewParameter();
        StairsRoomsAreNeverGeneratedDark();
        DarkRoomWallsAreNeverMarkedDarkThemselves();
        WallBorderingADarkRoomIsVisibleToASightedObserverButNotABlindOne();
        DarkRoomSharedWallAndDoorDiscoveryEndToEndScenario();
        ASightedObserverCannotSeeAcrossADarkRoomsOpenInteriorToItsFarBoundary();
        FovSeesEveryOpenTileWithinRadiusAndNothingBeyondIt();
        FovStopsAtAWallButStillShowsTheWallItself();
        FovConnectivityFilterNeverLeaksIntoAFullyEnclosedRoomThroughATightCorner();
        RecomputeReachabilityMarksOnlyTheConnectedComponentAndItsBorderingWalls();
        FieldOfViewNeverReturnsAnUnreachableTileRegardlessOfRawLineOfSight();
        LightingNeverIlluminatesAnUnreachableTile();
        OldSaveWithAnAlreadyExploredOrphanedWallForgetsItOnLoad();
        BashRequiresAnEquippedShieldInEitherHand();
        BashWorksWithAShieldInThePrimaryHand();
        BashWorksWithAShieldInTheOffHand();
        ShieldDefenseBonusIncreasesBashDamageByExactlyThatAmount();
        AStrongerShieldProducesMoreBashDamageThanAWeakerShield();
        NonShieldArmorDoesNotContributeTheBashBonus();
        OrdinaryAttacksAndOtherWeaponDamageEffectSkillsIgnoreTheShieldBonus();
        CastersPhysicalAttackPowerIsRestoredAfterBashResolves();
        BashUnlockLevelDoublesForPriestFlooredAtFive();
        PriestAndWarriorEachGainBashAtTheirOwnUnlockLevelNotTheOthers();
        ThiefAndMageNeverGetBash();
        KnockdownResolverAppliesSetsProneAndConsumesABankedReadyAction();
        KnockdownResolverDoesNotPenalizeAnAlreadyProneTargetTwice();
        KnockdownResolverIsResistedByCrowdControlImmunity();
        EnvironmentalFallBypassesCrowdControlImmunityAndNeverTouchesEnergy();
        ConsumeReadyActionIfBankedOnlyRemovesEnergyWhenAnActionIsActuallyBanked();
        StandClearsProneAndConsumesATurn();
        StandWhileAlreadyStandingIsFreeAndReportsSo();
        IsAllowedWhileProneAllowsOnlyInformationalCommandsAndRejectsEverythingElse();
        ResolveNonPlayerTurnAutoStandsAProneMonsterInsteadOfRunningAi();
        ResolveNonPlayerTurnPrioritizesStunOverProne();
        BashKnocksDownATargetItHits();
        BashDoesNotKnockDownATargetItMisses();
        BashNoLongerAppliesTheOldStun();
        TripIsGrantedOnlyToTheThiefAtLevelSeven();
        TripHasNoWeaponAttackRoll();
        TripKnocksDownWithoutDealingDamage();
        TripStillConsumesCooldownOnAFailedRoll();
        TremorIsOnlyAvailableToMage();
        TremorDealsNoDamageAndKnocksDownAtRange();
        TremorFailedRollLeavesTargetStandingWithFlavorMessage();
        SlipChanceFormulaMatchesWorkedExamples();
        BarefootDoublesTheSlipChance();
        NormalFloorNeverTriggersASlipRoll();
        WaterUsesALowerBaseChanceThanIce();
        SlipChanceLuckModifierStaysWithinItsConfiguredClampBounds();
        RepeatedlyMovingOntoIceCanKnockThePlayerProneViaHandleMove();
        MovingOntoNormalFloorNeverTriggersASlipRegardlessOfIterationCount();
        PlayerIsProneSurvivesASaveLoadRoundTrip();
        MonsterRestoreDataAppliesIsProne();
        LookIndicatesWhenATargetIsProne();
        EachNewPriestSkillIsGrantedAtItsIntendedLevelNotEarlier();
        NoneOfTheSixNewPriestSkillsIsGrantedToAnyOtherClass();
        PriestBashRemainsUnchangedAtLevelFive();
        PlayerIntercessionStateSurvivesASaveLoadRoundTrip();
        MonsterFrightenedUntilTurnSurvivesRestoreData();
        LayOnHandsHealsTenPlusAdjustedWisdomBeforeProficiencyScaling();
        LayOnHandsNeverExceedsMaximumHealth();
        LayOnHandsAtFullHealthIsRejectedWithoutConsumingATurnOrCooldown();
        LayOnHandsWorksWhileSilenced();
        LayOnHandsConsumesItsCooldownOnSuccess();
        TurnUndeadOnlyAffectsVisibleUndeadWithinRadius();
        TurnUndeadEachTargetResolvesIndependentlyAndSkipsNonUndead();
        TurnUndeadDoesNotAffectLivingCreatures();
        TurnUndeadBossResistancePenaltyReducesTheChanceWithoutEliminatingIt();
        TurnUndeadWithNoEligibleTargetsIsRejectedWithoutConsumingATurnOrCooldown();
        TurnUndeadStartsCooldownEvenWhenEveryTargetResists();
        FrightenedActorFleesAwayFromTheSourceInsteadOfActingNormally();
        FrightenedActorNeverEndsUpCloserToTheSource();
        FrightenedActorAdjacentToTheSourceStillActsInsteadOfFreezing();
        PurifyRemovesAHostileStatusEffectButNotASelfBuff();
        PurifyRemovesSilencedAndFrightenedButNotStunnedOrProne();
        PurifyWithNothingToRemoveIsRejectedWithoutConsumingATurnOrCooldown();
        PurifyRemovesMultipleEligibleConditionsInOneUse();
        SteadfastResistanceChanceIsExactlyTwentyFivePercent();
        SteadfastCanResistKnockdownStunAndFrightened();
        SteadfastNeverAppliesToAMonster();
        SteadfastDoesNotResistAnEnvironmentalFall();
        CrowdControlImmunityTakesPriorityOverSteadfast();
        ExorcismRequiresAnAdjacentTarget();
        ExorcismRequiresAMeleeWeapon();
        ExorcismRejectsANonUndeadTarget();
        ExorcismMissApplesNoPhysicalOrHolyDamage();
        ExorcismHitAppliesBothPhysicalAndHolyDamage();
        ExorcismHitAppliesANonZeroHolyBonusOnTopOfThePhysicalHit();
        IntercessionPreventsLethalDamageLeavingExactlyOneHp();
        IntercessionDoesNotTriggerOnNonLethalDamage();
        LaterLethalDamageAfterInterceptionIsConsumedKillsNormally();
        IntercessionExpiresAfterItsDurationWithNoLethalDamage();
        RecastingIntercessionWhileActiveIsRejectedWithoutConsumingATurnOrCooldown();
        IntercessionActivatesWithTheCorrectDurationAndFlavorMessage();
        EveryDarkRoomEntranceHasADoor();
        BossRoomSpurNeverLeavesADarkRoomHostUndoored();
        RoomObjectBlocksActorMovementOnlyWhenBlocksMovementIsTrue();
        IsBlockedForObjectPlacementRejectsEveryAmbiguousDestination();
        IsBlockedForActorMovementToleratesTrapsAndItemsButNotChestsThatObjectPlacementRejects();
        ChestCanBeOpenedClosedAndReopenedRemainingFindable();
        SuccessfullyPickedChestLockStaysUnlockedPermanently();
        ContainerRulesEnforceSizeEligibilityAndScrollSpellbookException();
        ContainerRulesRejectNestingAtEveryLevel();
        ContainerWeightCalculatorMatchesTheProposalsWorkedExamples();
        SlotCountingTreatsAMergedStackAsOneSlotAndAPartialRemainderAsNeedingAFreeOne();
        ContainerTransferServiceBlocksRemovalThatWouldExceedCapacityButNeverBlocksPutting();
        PickingUpAFilledBagUsesItsCompleteEffectiveWeight();
        DestroyingAFlammableBagSpillsSurvivingContentsWithoutDuplication();
        NonEmptyBagCannotBeSoldButAnEmptyOneCan();
        TwoSpawnedBagsNeverShareAContentsList();
        OldChestDataWithoutContainerStillLoadsAndRemainsAccessible();
        SameNamedContainerVariantsResolveToTheCorrectSlotCapacityOnLoad();
        LegacyPreRenameContainerNamesStillResolveAfterTheNameShortening();
        RemovingAnItemFromAContainerNeverAutoOffersToEquipIt();
        ContainerContentsParticipateInComparisonRespectingLocation();
        NonContainerInventoryItemsExcludesCarriedBags();
        ContainerScreenInventoryPaneOnlyShowsItemsThatFitTheOpenContainer();
        MarginTextWriterPrefixesEveryLineAndScreenMarginNestsSafely();
        ChaseAiNeverStepsOntoABlockingRoomObject();
        ChaseAiRoutesAroundABlockingRoomObjectWhenSpaceAllows();
        ChaseAiRoutesAroundAStationaryTraderBlockingTheDirectPath();
        RoomObjectTriggerFiresOnlyUnderItsConfiguredCondition();
        OneShotRoomObjectTriggerNeverFiresTwice();
        RepeatableRoomObjectTriggerCanFireAgainAfterActivating();
        RevealHiddenDoorEffectCarvesTheWallAndAddsAnUnlockedDoor();
        SpawnCreaturesEffectSkipsAlreadyOccupiedPositions();
        RoomObjectDataRoundTripPreservesPositionMovabilityAndTriggerState();
        RoomObjectWithNoTriggerRoundTripsWithNullTrigger();
        OlderLevelSaveDataWithoutRoomObjectsFieldStillLoadsWithAnEmptyList();
        GeneratedRoomObjectsNeverOccupyAnInvalidOrAmbiguousTile();
        GeneratedGroundItemsAndChestsNeverOccupyTheStairsTiles();
        WrapTextNeverProducesALineLongerThanTheGivenWidth();
        WrapTextNeverDropsAnyWordsRegardlessOfDescriptionLength();
        GeneratedMovableRoomObjectsAlwaysHaveAtLeastOneLegalPushDirection();
        LookHereReportsNothingUnusualOnAPlainOpenFloorTile();
        LookHereListsNonNormalFloorTypeAndItemsOnTheCurrentTile();
        LookHereShowsDarkAndWithholdsItemsWhileStandingOnAnUnilluminatedDarkTile();
        DetectTrapsChanceForStatMatchesTheEstablishedStatBasedFormula();
        DetectTrapsEffectOpensATimedWindowOnTheGivenStat();
        RollDetectTrapsNeverRevealsAnythingOnceItsWindowHasExpired();
        RollDetectTrapsEventuallyRevealsANearbyTrapWhileItsWindowIsOpen();
        TriggeringATrapMessageUsesSeverityWordNotRawNumber();
        AwardDeathRewardsIncrementsMonstersKilledOnlyForACreditedDeath();
        AwardDeathRewardsCreditsAPlayerOwnedDotKillAsAKill();
        AwardDeathRewardsTracksBossKillsAndTheHighestLevelBossRecord();
        CombatStatsTrackerRecordsActualDamageDealtAndExcludesOverkill();
        CombatStatsTrackerRecordsActualDamageTakenAndExcludesOverkill();
        CombatStatsTrackerHealingNeverReducesDamageTaken();
        CombatStatsTrackerNeverCreditsDamageDealtWhenOwnerIsNotThePlayer();
        CollectGoldAndSpendGoldUpdateAdventureRecordOnlyOnSuccess();
        RestoreGoldNeverCountsAsGoldCollected();
        TraderPurchaseAndIdentificationIncreaseGoldSpent();
        TraderSaleIncreasesGoldCollected();
        DeepestFloorReachedNeverDecreasesAfterAscending();
        AdventureRecordDataRoundTripPreservesEveryField();
        OlderSaveDataWithoutAdventureRecordFieldStillLoadsWithAZeroedRecord();
        TimeInDungeonStartsAtZeroForAFreshCharacter();
        FormatTimeInDungeonProducesHoursMinutesSeconds();
        RecordElapsedDungeonTimeAccumulatesRealElapsedSeconds();
        RecordElapsedDungeonTimeResetsTheSessionStartSoItDoesNotDoubleCount();
        PageCountComputesTheExpectedNumberOfPagesAtEveryBoundary();
        SpellsAndSkillsPagesNeverExceedTheConfiguredPageSizeAndCoverEveryEntryExactlyOnce();
        FindMostValuableItemIncludesBothInventoryAndEquipment();
        FindMostValuableItemBreaksATieRandomly();
        FindMostValuableItemReturnsNoneWhenThePlayerOwnsNothing();
        AdventureRecordScreenBuildShowsNoneForBossAndItemWhenNeitherExists();
        EveryItemSizeTierIsUsedSomewhereInTheCatalog();
        ItemSizeMappingMatchesTheDesignedMonsterLootBridge();
        ALargeMonsterCanDropAVeryLargeItemButASmallMonsterCannotDropMedium();
        ThrownLossChanceIsHigherOnAMissThanAHitForSmallSizes();
        MediumAndLargerItemsHaveNoThrownLossOrMisplacedChanceInStage1();
        ConcealmentDifficultyReflectsTerrainDarknessIlluminationAndStanding();
        ReturningAndProtectedItemsCannotBeLost();
        UnidentifiedButValuableItemsAreStillProtectedByTheirRealValue();
        DroppedBundleQuantityDiscountsMatchDesign();
        ConsumeManyPeelsOffOnlyTheChosenQuantity();
        FireAndLavaDestructionTakesPriorityOverALandingRollForBothThrownAndDropped();
        AGuaranteedSurvivingThrownItemLandsOnTheFloorExactlyOnce();
        ALowRollAgainstANonzeroChanceLosesTheItemAndNeverPlacesIt();
        AMidRangeRollAgainstANonzeroChanceMisplacesTheItemInsteadOfLosingIt();
        EveryLandingProducesExactlyOneOfTheFourStage1Outcomes();
        ProtectedItemsCanBeMisplacedButNeverPermanentlyLost();
        PermanentLossUpdatesTheMostValuableLostItemAndTiesKeepTheExistingRecord();
        StackedQuantityDropsCountIndividualUnitsNotEntries();
        ConcealedItemsAreExcludedFromGetItemAtGetItemsAtAndPickupChoices();
        ActiveSearchRevealsAnInRangeConcealedItemAndAlwaysConsumesATurn();
        FailedActiveSearchLeavesTheItemConcealedButStillConsumesATurn();
        PassiveDiscoveryOnIlluminationIsOneShotPerItem();
        RecoveryCounterIncrementsExactlyOnceAndRepeatedDropPickupCyclesDoNotDoubleCount();
        ConcealedGroundItemStateAndAdventureRecordItemStatsSurviveSaveLoad();
        OldSaveGroundItemsWithoutConcealmentFieldsLoadAsVisible();
        UnidentifiedItemLossMessagesNeverRevealTheTrueName();
        LoadingGroundItemsNeverRerollsLoss();
        OldSaveItemsResolveItemSizeFromTheCurrentCatalog();
        ConsolidateStacksMergesSplitAmmoIntoOneRunningCount();
        ConsolidateStacksRespectsThePotionCapWhenMergingMultipleStacks();
        ConsolidateStacksNeverMergesDifferentlyIdentifiedCopies();
        SellingAPartialQuantityLeavesTheRestOfTheStackBehind();
        SellingWithNoQuantityGivenSellsTheWholeStackAsBefore();
        SellingAPartialQuantityPricesOnlyThatPortion();
        WearableItemWithAnEmptyValidSlotIsEligibleForTheEquipOffer();
        AcceptingTheEquipOfferEquipsItAndRemovesItFromInventory();
        DecliningTheEquipOfferLeavesItInInventory();
        TheEquipOfferNeverChangesIsIdentified();
        TheEquipOfferPromptAndMessageNeverRevealAnUnidentifiedItemsTrueName();
        TheEquipOfferUsesTheRealDisplayNameWhenIdentified();
        NonEquipmentItemsAreIneligibleForTheEquipOffer();
        ClassRestrictedEquipmentIsIneligibleForTheEquipOffer();
        LevelRestrictedEquipmentIsIneligibleForTheEquipOffer();
        RaceRestrictedEquipmentIsIneligibleForTheEquipOffer();
        AnOccupiedSingleSlotPreventsTheEquipOfferAndNeverPromptsToReplace();
        ARingEquipOfferSelectsTheFirstEligibleEmptyRingSlot();
        AHandItemEquipOfferSelectsTheFirstEligibleEmptyHandSlot();
        ASecondWeaponIsIneligibleForTheEquipOfferWithoutDualWield();
        ASecondWeaponIsEligibleForTheEquipOfferWithDualWield();
        GetAllNeverOffersToEquipPickedUpItems();
        FailedPickupNeverOffersToEquip();
        AcceptingTheEquipOfferDoesNotConsumeAnAdditionalTurn();
        MergedStackableAmmoStillEligibleForTheEquipOffer();
        SomeStackableItemsAreAlsoEquipment();
        AHandItemIsStillOfferedWithALauncherEquippedAndAShieldInPrimaryHand();
        BowEquipsOnlyInRangedWeaponSlot();
        CrossbowEquipsOnlyInRangedWeaponSlot();
        SlingEquipsOnlyInRangedWeaponSlot();
        ArrowEquipsOnlyInAmmunitionSlot();
        DedicatedThrowingWeaponsCannotEquipInAHand();
        DedicatedThrowingWeaponsEquipInAmmunitionSlot();
        ShortSpearCanEquipInAHandOrAmmunitionSlotPreferringAmmunition();
        LongSpearCanEquipInAHandOrAmmunitionSlotPreferringAHand();
        EquippingALauncherNeverAffectsMeleeDamageOrAttackType();
        LauncherWithoutAHandWeaponUsesUnarmedAttackType();
        AHandEquippedSpearAffectsMeleeNormally();
        AnAmmoThrownEquippedSpearDoesNotAffectMelee();
        BowFiresArrowsThroughTheLauncherPath();
        BowCannotFireBolts();
        CrossbowFiresBoltsThroughTheLauncherPath();
        CrossbowCannotFireArrows();
        SlingFiresSlingStonesAndRocksThroughTheLauncherPath();
        RockFiredFromASlingTravelsLessFarThanAProperSlingStone();
        ArrowCannotBeFiredWithoutABow();
        DartCanBeThrownWithoutALauncherAndWhileABowIsEquipped();
        EveryClassCanThrowLightItemsRegardlessOfItsAllowedThrowableCategoriesList();
        NothingReadiedIsRejectedWithoutConsumingATurn();
        AmmunitionAndLauncherBothContributeToFiredDamage();
        LauncherCannotDealProjectileDamageWithoutAmmunition();
        ShortSpearHasLongerThrowingRangeThanLongSpear();
        LongSpearDealsMoreMeleeDamageThanShortSpear();
        EquippingOrUnequippingRangedWeaponOrAmmunitionChangesObservableSlotState();
        EquippingAnUnrelatedSlotNeverChangesRangedOrAmmunitionState();
        FiringOrThrowingRemovesExactlyOneUnitFromTheReadiedStack();
        TheFinalShotClearsTheAmmunitionSlot();
        ShortSpearsCanStackButLongSpearsRemainIndividual();
        DifferentlyIdentifiedAmmoStacksNeverMerge();
        ReadyingMoreOfTheSameAmmoMergesIntoTheEquippedStackInstead();
        EquippingAmmunitionNeverDuplicatesOrDeletesQuantity();
        FiredAmmoEntersTheLandingResolverAndCanBeRecovered();
        EnvironmentalDestructionTakesPriorityOverBreakage();
        BreakageTakesPriorityOverLossOrConcealment();
        BrokenItemsAreNeverPlacedOnTheFloor();
        BreakingAnItemIncrementsItemsBroken();
        RocksAndSlingStonesNeverBreakUnderTheirDefaultConfiguration();
        ReturningWeaponsRemainProtectedFromBreakage();
        WallImpactCreatureHitAndOrdinaryLandingUseDifferentConfiguredChances();
        FiringOrThrowingUnidentifiedAmmoNeverIdentifiesIt();
        LandingAndRecoveringAnUnidentifiedItemNeverIdentifiesIt();
        BreakageMessagesNeverRevealAnUnidentifiedItemsTrueName();
        FiringErrorMessagesNeverRevealAnUnidentifiedItemsTrueName();
        EquippedItemDataRoundTripsForTheNewSlotsJustLikeAnyOther();
        OlderGroundItemDataWithoutTheNewFieldsStillDefaultsSafely();
        MonsterRangedGearUpEquipsBothNewSlotsDirectly();

        // --- Sleep command ---
        ResolveCommandMatchesOldReadCommandBehaviorForAFewKeys();
        WBindsToSleepAndItsOldReservedCommentIsGone();
        EncounterSizeCalculatorAlwaysRollsBetweenOneAndFive();
        AmbushChanceDecreasesAsLuckIncreasesAndStaysClamped();
        BossAmbushChanceDecreasesAsLuckIncreasesAndStaysClamped();
        FindAmbushPositionsNeverReturnsABlockedOrOutOfRangeTile();
        FindAmbushPositionsPrefersUnseenAndNonAdjacentTiles();
        FindAmbushPositionsReturnsFewerThanRequestedWhenTilesAreScarce();
        RecoveryPhraseTierBoundariesMatchTheDesignDoc();
        SleepIsRejectedWhileAHarmfulEffectIsActive();
        SleepIsRejectedOnARecurringDamageTile();
        SleepIsRejectedWhenAlreadyFullyRested();
        StartingSleepAppliesFourTimesTheNormalRegenRate();
        TakingDamageWhileAsleepEndsSleepWithARecoveryMessage();
        AScheduledAmbushFiresAtTheRightTurnAndRegistersWithTheLevel();
        HandleSleepInputKeyOpensHelpWithoutWaking();
        HandleSleepInputKeyIgnoresUnrecognizedKeys();
        HandleSleepInputKeyWakesOnAnyOtherRecognizedCommand();

        // --- Ability Proficiency System ------------------------------------------------------
        ProficiencyRankThresholdBoundariesMatchSpec();
        StandardMultiplierMatchesSpecAtAllFiveRanks();
        PercentagePointAdjustmentMatchesSpecAtAllFiveRanks();
        SecondaryEffectChanceMatchesSpecAtAllFiveRanks();
        D20AdjustmentMatchesSpecAtAllFiveRanks();
        BespokeProficiencyTablesMatchSpec();
        AptitudeAndCatalogLevelLearningRatesMatchSpecWorkedExamples();
        LuckyInsightChanceFormulaClampsAndScalesWithLuck();
        ChallengeMultiplierMatchesSpecGapBuckets();
        RankScalingNeutralReproducesTodaysUnscaledNumbers();
        RankScalingScaleDurationRoundsAndNeverGoesBelowOne();
        RankScalingScaleMagnitudeHandlesNegativeValuesCorrectly();
        EveryRankedSkillAndSpellHasAGoverningAttributeAndEveryUnrankedOneDoesNot();
        SharedSpellsLeaveGoverningAttributeNullForDynamicResolution();
        ProficiencyTrackerAwardsPointsAndRanksUpExactlyAtThreshold();
        ProficiencyTrackerNeverAwardsPointsOnAFailedCast();
        ProficiencyTrackerStopsAwardingOnceMaster();
        ProficiencyTrackerLuckyInsightAddsAFlatBonusAndNeverSkipsARank();
        ProficiencyTrackerBailsOnAFailedStatBasedRollWithoutAwardingPoints();
        BashStunBecomesProbabilisticAtNoviceRankButAlwaysAppliesAtMaster();
        MessageLogAddDefaultsToWhiteAndAcceptsAnExplicitColor();
        RenderStatusBarPreservesPerMessageColorAcrossWrappedLines();
        PlayerAbilityStateSurvivesASaveLoadRoundTrip();
        OldSaveDataWithoutAbilityStateFallsBackToLevelBasedSkillRederivation();

        // --- Pet and Companion System ---
        PetProgressionStatsMatchTheDesignDocsWorkedTable();
        SyncToLevelPreservesHealthPercentageAndGuaranteesAtLeastOneHeal();
        OwnerLevelingUpResyncsThePetAutomatically();
        CreateDogProducesTheExpectedLevelOneIdentity();
        AllegianceTreatsAnOwnerAndItsOwnPetAsFriendly();
        BumpingIntoYourOwnPetNeverAttacksItAndDisplacesItInstead();
        BumpingIntoAPetWithNoRoomToStepAsideFailsWithoutConsumingATurn();
        ADisplacedPetPrefersASafeTileOverAHazardousOne();
        UKeyResolvesToTheSwapWithPetCommand();
        SwappingWithAnAdjacentPetTradesTilesExactly();
        SwappingWithAPetTooFarAwayFailsWithoutConsumingATurn();
        SwappingWithNoPetAtAllFailsGracefully();
        IdleRoamingPrefersSafeTilesOverHazardousOnesWhenPossible();
        ReturningToOwnerPrefersASafeStepOverAHazardousDiagonalOne();
        APetStandingOnFireTakesEnvironmentalDamageLikeAMonster();
        HostileTargetSelectorPrefersWhicheverIsCloser();
        HostileTargetSelectorFallsBackToThePlayerWithNoPet();
        PetAttacksAnAdjacentHostileMonster();
        PetMovesTowardAReachableTargetWhenNotYetAdjacent();
        RoamingAreaMembershipMatchesTheThreeTileChebyshevLimit();
        AnIdlePetInsideTheAreaOnlyEverPicksDestinationsThatStayInsideIt();
        AnIdlePetSometimesRemainsStillAndNeverEntersAnOccupiedOrBlockedTile();
        OwnerMovementCanPushAPreviouslyInRangePetOutOfRangeAndItBeginsReturning();
        ReturnBehaviorStopsAssoonAsThePetReentersTheAreaAndRoamingResumes();
        APetMayLeaveTheRoamingAreaWhilePursuingAValidCombatTarget();
        APetWithAMonsterOwnerCentersItsRoamingAreaOnThatMonster();
        PetTargetsWhateverItsOwnerMostRecentlyAttacked();
        PetTargetsWhateverMostRecentlyAttackedItsOwner();
        PetAbandonsADeadTargetAndReacquires();
        PetAbandonsAnUnreachableTargetForTheNearestReachableThreat();
        FriendlyFireNeverHitsYourOwnPetWithATileAreaOfEffect();
        FriendlyFireRejectsASingleTargetSpellAimedAtYourOwnPet();
        ProjectilesFromThePlayerNeverHitTheirOwnPet();
        ProjectilesFromAMonsterCanHitThePlayersPet();
        AMonsterKilledByThePlayersPetGrantsRewardsToThePlayer();
        AMonsterKilledByAMonsterOwnedPetDoesNotRewardThePlayer();
        ADeadPetNeverAwardsXpGoldOrLoot();
        HandlePetDeathTransitionsToAwaitingRespawnWithTheCorrectDeadline();
        RespawningRestoresFullHealthCorrectLevelStatsAndActiveStatus();
        ALivingPetFollowsItsOwnerThroughStairs();
        APetAwaitingRespawnIsUnaffectedByStairTravel();
        PetRoundTripsThroughSaveAndLoadPreservingLifecycleState();
        PetAwaitingRespawnSurvivesASaveLoadRoundTrip();
        OldSaveWithNoPetDataLeavesThePlayerWithoutOne();

        // --- Corpse System ---
        ADeadMonsterWithLootCreatesExactlyOneLootBearingCorpse();
        ADeadMonsterWithNoLootCreatesAnImmediatelyPortableCorpse();
        TheCorpseRecordsTheStableDefinitionNotDisplayText();
        BossCorpseNameHasNoArticleOrdinaryCorpseDoes();
        CorpseWeightIsDerivedFromOriginalSizeAndFrozen();
        CorpseItemsAreNeverStackable();
        CorpsesNeverBlockMovement();
        MultipleCorpsesCanShareOneTile();
        OpeningTheOnlyCorpseOnATileOpensItDirectly();
        OpeningWithNoChestOrCorpseFailsCleanly();
        EmptyingTheLastItemConvertsTheCorpseToAPortableItem();
        APortableCorpseCanBePickedUpLikeAnOrdinaryItem();
        ADeadPetLeavesACorpseWithPetOriginAndOwnerName();
        ADeadMonstersKillCreditAndCorpseAreIndependent();
        CorpseMetadataSurvivesASaveLoadRoundTripForALootBearingCorpse();
        PortableCorpseMetadataSurvivesASaveLoadRoundTrip();
        OldSaveWithNoCorpseDataLoadsWithAnEmptyCorpseList();
        CorpseSelectionLabelsDistinguishIdenticalNamesOnlyWithinTheMenu();

        // --- Safe Monster Spawning and Hazard-Aware Movement ---
        OrdinaryMonstersAreUnsafeOnFireOrLava();
        OrdinaryMonstersAreSafeOnHarmlessSpecialFloors();
        FireAttunedMonsterIsSafeOnFireButNotOnLava();
        LavaAttunedMonsterIsSafeOnLava();
        MatchingTerrainZeroesTheEnvironmentalTickWithoutGrantingGeneralFireImmunity();
        AFloorAttunedMonsterOffItsPreferredTerrainIsUnsafe();
        CanActorOccupyCombinesPhysicalBlockingWithSafety();
        FindSafeAdjacentTilePrefersSafetyAndReturnsNullWhenNoneQualifies();
        FindNextStepTreatsHazardousTerrainAsFullyImpassableForAnActor();
        FindNextStepRoutesAroundHazardWhenASafeDetourExists();
        AChasingMonsterRemainsInPlaceWhenEveryRouteIsHazardous();
        APetNeverSpawnsRoamsOrPathsOntoDamagingTerrain();
        SleepAmbushNeverPlacesAMonsterOnFireOrLava();
        GeneratedFloorsNeverPlaceAnOrdinaryMonsterOnFireOrLava();
        PlayerMovementOntoFireOrLavaRemainsAllowed();

        // --- Container "Get All" ---
        GetAllMovesEveryContainerItemIntoInventory();
        GetAllOnAnEmptyContainerReportsNothingToTake();
        GetAllStopsAtCarryingCapacityAndLeavesTheRestInTheContainer();
        GetAllFromACorpseEmptiesItAndGameLoopConvertsItToAPortableItem();
        MenuPromptIndexFromKeyMapsDigitsAndLettersForTheFullOptionRange();

        Console.WriteLine();
        Console.WriteLine($"=== {passed} passed, {failed} failed, {skipped} skipped ===");
        return failed == 0 ? 0 : 1;
    }

    private static void Check(string name, bool condition)
    {
        if (condition)
        {
            passed++;
            Console.WriteLine($"[PASS] {name}");
        }
        else
        {
            failed++;
            Console.WriteLine($"[FAIL] {name}");
        }
    }

    private static void Skip(string name, string reason)
    {
        skipped++;
        Console.WriteLine($"[SKIP] {name} ({reason})");
    }

    // --- TurnScheduler ---------------------------------------------------------------

    /// <summary>
    /// Regression test for a real hang: a low-Speed actor that reaches exactly the
    /// threshold and then never gets picked (because other actors keep momentarily
    /// overshooting past it) used to freeze there forever -- see GetNextActor's own
    /// comment on lastOffered. A boss room's 4-8 entourage monsters made this dramatically
    /// more likely to occur in real play than a normal, sparser floor ever did.
    /// </summary>
    private static void SchedulerNeverStarvesAFrozenLowSpeedActor()
    {
        var scheduler = new TurnScheduler();

        var slowActor = Monster.CreateRandom(0, 0, 1, new Random(1));
        slowActor.Speed = 9;
        slowActor.Energy = 100;
        scheduler.Register(slowActor);

        for (int i = 0; i < 8; i++)
        {
            var fastMonster = Monster.CreateRandom(i + 1, 0, 1, new Random(100 + i));
            fastMonster.Speed = 30;
            fastMonster.Energy = 0;
            scheduler.Register(fastMonster);
        }

        bool slowActorEventuallyActed = false;
        for (int i = 0; i < 500; i++)
        {
            var chosen = scheduler.GetNextActor();
            if (chosen == slowActor)
            {
                slowActorEventuallyActed = true;
                break;
            }
            scheduler.ConsumeEnergy(chosen);
        }

        Check("A low-speed actor frozen at exactly the threshold isn't starved forever by several faster actors", slowActorEventuallyActed);
    }

    /// <summary>
    /// The other half of the lastOffered invariant: re-polling for the same still-pending
    /// turn (a blocked move, a cancelled menu -- anything that returns without calling
    /// ConsumeEnergy) must keep returning the same actor without banking it extra energy,
    /// or its own speed pacing would break.
    /// </summary>
    private static void SchedulerDoesNotInflateEnergyOnFreeRepolls()
    {
        var scheduler = new TurnScheduler();

        var actingActor = Monster.CreateRandom(0, 0, 1, new Random(2));
        actingActor.Speed = 10;
        actingActor.Energy = 100;
        scheduler.Register(actingActor);

        var otherActor = Monster.CreateRandom(1, 0, 1, new Random(3));
        otherActor.Speed = 5;
        otherActor.Energy = 0;
        scheduler.Register(otherActor);

        var first = scheduler.GetNextActor();
        int energyAfterFirstPoll = first.Energy;

        var second = scheduler.GetNextActor();

        Check("Re-polling the same pending actor without consuming its turn returns it again", second == first);
        Check("Re-polling the same pending actor without consuming its turn doesn't bank it extra energy", second.Energy == energyAfterFirstPoll);
    }

    // --- CreatureType / capabilities -----------------------------------------------

    private static void CreatureCapabilityDefaults()
    {
        var rng = new Random(1);
        int humanoidCount = 0, animalCount = 0, undeadCount = 0;
        bool humanoidOk = true, animalOk = true, undeadOk = true;

        for (int i = 0; i < 500; i++)
        {
            var m = Monster.CreateRandom(0, 0, difficultyLevel: 5, rng);
            switch (m.CreatureType)
            {
                case CreatureType.Humanoid:
                    humanoidCount++;
                    humanoidOk &= m.CanCarryItems && m.CanEquipItems && m.CanUseItems;
                    break;
                case CreatureType.Animal:
                    animalCount++;
                    animalOk &= !m.CanCarryItems && !m.CanEquipItems && !m.CanUseItems;
                    break;
                case CreatureType.Undead:
                    undeadCount++;
                    undeadOk &= !m.CanCarryItems && !m.CanEquipItems && !m.CanUseItems;
                    break;
            }
        }

        Check("Humanoid archetypes spawn with full item capabilities", humanoidCount > 0 && humanoidOk);
        Check("Animal archetypes spawn with no item capabilities", animalCount > 0 && animalOk);
        Check("Undead archetypes spawn with no item capabilities", undeadCount > 0 && undeadOk);
    }

    private static void LootGeneratorGating()
    {
        var rng = new Random(2);
        var player = new Player("Tester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng)) { Level = 5 };

        Monster animal = null;
        for (int i = 0; i < 300 && animal == null; i++)
        {
            var m = Monster.CreateRandom(0, 0, 5, rng);
            if (m.CreatureType == CreatureType.Animal)
            {
                animal = m;
            }
        }

        bool everDropped = false;
        for (int i = 0; i < 100; i++)
        {
            if (animal != null && (LootGenerator.GenerateLoot(animal, player, rng).Count > 0 || LootGenerator.GenerateSpawnItems(animal, rng).Count > 0))
            {
                everDropped = true;
            }
        }

        Check("An Animal-type monster never generates death loot or spawn items", animal != null && !everDropped);
    }

    // --- Combat: blessed/undead, Protection -----------------------------------------

    private static Monster.RestoreData BaseRestoreData(string name, int attack, int defense, CreatureType type = CreatureType.Other) => new()
    {
        Name = name,
        Symbol = '?',
        Color = ConsoleColor.White,
        ShortDescription = name,
        LongDescription = name,
        Size = Size.Medium,
        AttackType = AttackType.Hit,
        CreatureType = type,
        BasePhysicalAttackPower = attack,
        DefensePower = defense,
        MaxHp = 1000,
        CurrentHp = 1000,
        MaxMana = 0,
        CurrentMana = 0,
        Agility = 5
    };

    private static void BlessedWeaponVsUndead()
    {
        var rng = new Random(3);
        var attacker = Monster.Restore(BaseRestoreData("attacker", attack: 10, defense: 0));
        attacker.Equipment.EquipInSlot(EquipmentSlot.PrimaryHand, Items.BlessedLongSword);

        var undead = Monster.Restore(BaseRestoreData("undead-target", attack: 0, defense: 0, CreatureType.Undead));
        var normal = Monster.Restore(BaseRestoreData("normal-target", attack: 0, defense: 0, CreatureType.Other));

        double avgVsUndead = AverageHitDamage(attacker, undead, rng, samples: 40);
        double avgVsNormal = AverageHitDamage(attacker, normal, rng, samples: 40);

        Check("A blessed weapon deals its bonus damage only against Undead targets",
            Math.Abs((avgVsUndead - avgVsNormal) - Items.BlessedLongSword.BlessedUndeadDamageBonus) < 1.0);
    }

    private static void ProtectionMitigation()
    {
        var rng = new Random(4);
        var attacker = Monster.Restore(BaseRestoreData("attacker2", attack: 20, defense: 0));

        var unprotected = Monster.Restore(BaseRestoreData("unprotected", attack: 0, defense: 0));
        var protectedTarget = Monster.Restore(BaseRestoreData("protected", attack: 0, defense: 0));
        var protectionItem = new Item("Test Plate", '[', "test", ItemType.Armor, EquipmentType.Body,
            statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Protection, magnitude: 4) });
        protectedTarget.Equipment.EquipInSlot(EquipmentSlot.Body, protectionItem);

        double avgUnprotected = AverageHitDamage(attacker, unprotected, rng, samples: 40);
        double avgProtected = AverageHitDamage(attacker, protectedTarget, rng, samples: 40);

        Check("Protection reduces incoming physical damage by its magnitude",
            Math.Abs((avgUnprotected - avgProtected) - 4) < 1.0);
    }

    private static double AverageHitDamage(Monster attacker, Monster defender, Random rng, int samples)
    {
        var damages = new List<int>();
        for (int i = 0; i < samples * 10 && damages.Count < samples; i++)
        {
            defender.Health.SetCurrent(defender.Health.Max);
            var result = attacker.PhysicalAttack(defender, rng);
            if (result.Hit)
            {
                damages.Add(result.Damage);
            }
        }
        return damages.Count == 0 ? -1 : damages.Average();
    }

    // --- On-hit item effects ---------------------------------------------------------

    private static (Monster attacker, Monster defender, Level level) BuildCombatPair(Item weapon = null, Item armor = null)
    {
        var level = new Level(1, 10, 10);
        var attacker = Monster.Restore(BaseRestoreData("proc-attacker", attack: 5, defense: 0));
        var defender = Monster.Restore(BaseRestoreData("proc-defender", attack: 0, defense: 0));
        if (weapon != null)
        {
            attacker.Equipment.EquipInSlot(EquipmentSlot.PrimaryHand, weapon);
        }
        if (armor != null)
        {
            defender.Equipment.EquipInSlot(EquipmentSlot.Body, armor);
        }
        level.Actors.Add(attacker);
        level.Actors.Add(defender);
        return (attacker, defender, level);
    }

    private static AttackResult ForceHit(Actor attacker, Actor defender) => new()
    {
        Attacker = attacker,
        Defender = defender,
        Hit = true,
        Damage = 5,
        Severity = HitSeverity.Minor,
        AttackType = AttackType.Hit
    };

    private static void OnHitInstantDamage()
    {
        var rng = new Random(5);
        var weapon = new Item("Test Flame", '/', "test", ItemType.Weapon, EquipmentType.Hand,
            statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Fire, chance: 1.0, magnitude: 7) });
        var (attacker, defender, level) = BuildCombatPair(weapon);

        int before = defender.Health.Current;
        string message = ItemEffectApplier.ApplyOnHitEffects(ForceHit(attacker, defender), level, rng);

        Check("An instant (Duration 0) Fire weapon effect deals immediate bonus damage",
            before - defender.Health.Current == 7);
        // Regression: "... takes 7 fire damage." used to leak the raw damage number instead of
        // a severity word like every other damage message in the game.
        Check("The instant status-effect damage message uses a severity word, never the raw damage number",
            message != null && !message.Any(char.IsDigit));
    }

    private static void OnHitDamageOverTime()
    {
        var rng = new Random(6);
        var weapon = new Item("Test Venom", '/', "test", ItemType.Weapon, EquipmentType.Hand,
            statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Poison, chance: 1.0, magnitude: 4, duration: 3) });
        var (attacker, defender, level) = BuildCombatPair(weapon);

        ItemEffectApplier.ApplyOnHitEffects(ForceHit(attacker, defender), level, rng);

        var poisonEffect = defender.ActiveEffects.FirstOrDefault(e => e.TickDamageType == DamageType.Poison);
        Check("A Duration > 0 Poison weapon effect applies a ticking condition instead of instant damage",
            poisonEffect != null && poisonEffect.TickDamage == 4 && poisonEffect.ExpiresOnTurn == level.TurnNumber + 3);
    }

    private static void OnHitStun()
    {
        var rng = new Random(7);
        var weapon = new Item("Test Mace", '/', "test", ItemType.Weapon, EquipmentType.Hand,
            statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Stun, chance: 1.0, duration: 3) });
        var (attacker, defender, level) = BuildCombatPair(weapon);

        ItemEffectApplier.ApplyOnHitEffects(ForceHit(attacker, defender), level, rng);

        Check("A Stun weapon effect sets StunnedUntilTurn", defender.StunnedUntilTurn == level.TurnNumber + 3);
    }

    private static void OnHitCorrode()
    {
        var rng = new Random(8);
        var weapon = new Item("Test Acid", '/', "test", ItemType.Weapon, EquipmentType.Hand,
            statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Corrode, chance: 1.0, magnitude: 3, duration: 5) });
        var (attacker, defender, level) = BuildCombatPair(weapon);

        int before = defender.DefensePower;
        ItemEffectApplier.ApplyOnHitEffects(ForceHit(attacker, defender), level, rng);

        Check("A Corrode weapon effect reduces DefensePower and registers a reverting ActiveEffect",
            before - defender.DefensePower == 3 && defender.ActiveEffects.Any(e => e.ModifiedStat == Stat.Armor && e.StatAmount == -3));
    }

    private static void ThornsReflection()
    {
        var rng = new Random(9);
        var armor = new Item("Test Spikes", '[', "test", ItemType.Armor, EquipmentType.Body,
            statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Thorns, magnitude: 6) });
        var (attacker, defender, level) = BuildCombatPair(armor: armor);

        int before = attacker.Health.Current;
        string message = ItemEffectApplier.ApplyOnHitEffects(ForceHit(attacker, defender), level, rng);

        Check("Thorns reflects damage back onto a melee attacker", before - attacker.Health.Current == 6);
        // Regression: "... takes 6 damage from thorns." used to leak the raw damage number
        // instead of a severity word like every other damage message in the game.
        Check("The thorns reflection message uses a severity word, never the raw damage number",
            message != null && !message.Any(char.IsDigit));
    }

    // --- EffectProcessor: resistance + regeneration -----------------------------------

    private static void EffectProcessorResistance()
    {
        var level = new Level(1, 10, 10);
        var target = Monster.Restore(BaseRestoreData("resist-target", attack: 0, defense: 0));
        var armor = new Item("Test Ward", '[', "test", ItemType.Armor, EquipmentType.Body,
            resistanceModifiers: new ResistanceSet(fire: 25));
        target.Equipment.EquipInSlot(EquipmentSlot.Body, armor);
        target.ActiveEffects.Add(new ActiveEffect("Test Burn", level.TurnNumber + 10) { TickDamage = 8, TickDamageType = DamageType.Fire });
        level.Actors.Add(target);

        int before = target.Health.Current;
        EffectProcessor.Tick(level);

        Check("Fire Resistance reduces a matching DoT tick by its percentage (8 * 0.75 = 6)", before - target.Health.Current == 6);
    }

    private static void EffectProcessorRegeneration()
    {
        var level = new Level(1, 10, 10);
        var target = Monster.Restore(BaseRestoreData("regen-target", attack: 0, defense: 0));
        var ring = new Item("Test Regen Ring", '=', "test", ItemType.Armor, EquipmentType.Ring,
            statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Regeneration, magnitude: 5) });
        target.Equipment.EquipInSlot(EquipmentSlot.PrimaryRing, ring);
        target.Health.SetCurrent(10);
        level.Actors.Add(target);

        EffectProcessor.Tick(level);

        Check("Regeneration heals a flat amount each tick while equipped", target.Health.Current == 15);
    }

    // --- Identify (spell) and per-instance identification -----------------------------

    private static void IdentifySpell()
    {
        var rng = new Random(10);
        var level = new Level(1, 10, 10);
        var caster = Monster.Restore(BaseRestoreData("identify-caster", attack: 0, defense: 0));
        caster.Mana = new ManaComponent(50);

        var item = Items.FlamingLongSword.Clone();
        item.IsIdentified = false;

        var context = new SpellCastingContext(caster, level, level.TurnNumber, rng) { TargetItem = item };
        var result = SpellCaster.Cast(SpellCatalog.Identify, context);

        Check("Casting Identify reveals the targeted item and reports its name",
            result.Success && item.IsIdentified && result.IdentifiedItemName == item.Name);
    }

    private static void PerInstanceIdentification()
    {
        var a = Items.CursedDagger.Clone();
        var b = Items.CursedDagger.Clone();
        a.IsIdentified = true;

        Check("Identifying one cloned instance never affects another instance or the shared template",
            a.IsIdentified && !b.IsIdentified && !Items.CursedDagger.IsIdentified);

        Check("DisplayName hides the true name until identified",
            b.DisplayName == $"Unidentified {b.Type}" && a.DisplayName == a.Name);
    }

    // --- InventoryScreen logic (the two methods bumped to internal for this) ----------

    private static void CursedItemBlocksUnequip()
    {
        var rng = new Random(11);
        var player = new Player("CurseTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));

        var identifiedCurse = Items.CursedDagger.Clone();
        identifiedCurse.IsIdentified = true;
        player.EquipInSlot(EquipmentSlot.PrimaryHand, identifiedCurse);
        string identifiedMessage = InventoryScreen.TryUnequip(player, EquipmentSlot.PrimaryHand);

        var unidentifiedCurse = Items.CursedDagger.Clone();
        player.EquipInSlot(EquipmentSlot.OffHand, unidentifiedCurse);
        string unidentifiedMessage = InventoryScreen.TryUnequip(player, EquipmentSlot.OffHand);

        Check("An identified cursed item's unequip attempt names the item",
            identifiedMessage.Contains(identifiedCurse.Name) && player.Equipment.Get(EquipmentSlot.PrimaryHand) == identifiedCurse);
        Check("An unidentified cursed item's unequip attempt stays vague",
            !unidentifiedMessage.Contains(unidentifiedCurse.Name) && player.Equipment.Get(EquipmentSlot.OffHand) == unidentifiedCurse);

        // RingOfStrength is never cursed -- confirms the non-cursed path still actually unequips.
        player.EquipInSlot(EquipmentSlot.PrimaryRing, Items.RingOfStrength);
        string normalMessage = InventoryScreen.TryUnequip(player, EquipmentSlot.PrimaryRing);
        Check("A non-cursed item unequips normally", player.Equipment.Get(EquipmentSlot.PrimaryRing) == null && normalMessage.StartsWith("You unequip"));
    }

    private static void UnidentifiedItemHidesDetails()
    {
        var rng = new Random(12);
        var player = new Player("DetailsTester", CharacterClass.Priest, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Priest, rng));

        var unidentified = Items.BlessedLongSword.Clone();
        string hiddenDetails = InventoryScreen.FormatItemDetails(player, unidentified);
        // Checks for the specific MECHANICAL markers the identified branch would add (not the bare
        // word "undead" -- BlessedLongSword's own flavor text already legitimately says "undead
        // flesh recoils from its edge" regardless of identification).
        Check("Unidentified item details show only the description plus a visible-quality line -- never Blessed/Cursed/procs (Item Comparison's Known-Information Rule)",
            hiddenDetails.StartsWith(unidentified.LongDescription) && hiddenDetails.Contains("(incomplete)")
            && !hiddenDetails.Contains("Blessed") && !hiddenDetails.Contains("damage vs. undead"));

        var identified = Items.BlessedLongSword.Clone();
        identified.IsIdentified = true;
        string revealedDetails = InventoryScreen.FormatItemDetails(player, identified);
        Check("Identified item details reveal Blessed status and the undead damage bonus",
            revealedDetails.Contains("Blessed") && revealedDetails.Contains("undead"));
    }

    // --- Item Comparison and Upgrade Recommendation System --------------------------------

    private static void SwordAndMaceShareCategoryButNotWithShieldOrLegArmor()
    {
        var rng = new Random(800);
        var player = new Player("CategoryTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var sword = Items.ShortSword.Clone();
        var mace = Items.Warhammer.Clone();
        var shield = Items.Shield.Clone();
        var boots = Items.LeatherBoots.Clone();

        Check("A sword and a mace (both Weapon-category hand items) can be compared",
            ItemComparisonCompatibility.CanCompare(sword, mace, player));
        Check("A sword cannot be compared with a shield", !ItemComparisonCompatibility.CanCompare(sword, shield, player));
        Check("A sword cannot be compared with foot armor", !ItemComparisonCompatibility.CanCompare(sword, boots, player));
    }

    private static void RingsAndArmorSlotsOnlyCompareWithinTheirOwnType()
    {
        var rng = new Random(801);
        var player = new Player("SlotIsolationTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var ring = Items.RingOfStrength.Clone();
        var otherRing = Items.RingOfStrength.Clone();
        var sword = Items.ShortSword.Clone();
        var gloves = Items.LeatherGloves.Clone();

        Check("A ring can be compared with another ring", ItemComparisonCompatibility.CanCompare(ring, otherRing, player));
        Check("A ring cannot be compared with a sword", !ItemComparisonCompatibility.CanCompare(ring, sword, player));
        Check("Gloves cannot be compared with a ring (each armor slot type is isolated from the others)",
            !ItemComparisonCompatibility.CanCompare(gloves, ring, player));
    }

    private static void HybridSpearClassifiesByItsActualEquippedSlot()
    {
        var rng = new Random(802);
        var playerInHand = new Player("SpearInHandTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var spearInHand = Items.ShortSpear.Clone();
        playerInHand.EquipInSlot(EquipmentSlot.PrimaryHand, spearInHand);
        var mace = Items.Warhammer.Clone();
        Check("A hybrid Spear equipped in a hand slot compares as a melee weapon",
            ItemComparisonCompatibility.CanCompare(spearInHand, mace, playerInHand));

        var rng2 = new Random(803);
        var playerInAmmo = new Player("SpearInAmmoTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng2));
        var spearInAmmo = Items.ShortSpear.Clone();
        playerInAmmo.EquipInSlot(EquipmentSlot.Ammunition, spearInAmmo);
        var mace2 = Items.Warhammer.Clone();
        Check("The same hybrid Spear readied in the Ammunition slot is never comparable with a sword/mace -- it cannot occupy that slot",
            !ItemComparisonCompatibility.CanCompare(spearInAmmo, mace2, playerInAmmo));
    }

    private static void ExpectedAndMaximumProcValuesReflectMagnitudeDurationAndChance()
    {
        var multiTurnProc = Items.VenomfangDagger.Clone();
        multiTurnProc.IsIdentified = true; // Poison: chance 0.25, magnitude 3, duration 3
        var multiTurnRating = ItemQualityCalculator.Evaluate(multiTurnProc);
        Check("A multi-turn proc's maximum value multiplies magnitude by duration", multiTurnRating.MaximumProcValue == 9m);
        Check("A multi-turn proc's expected value further multiplies by trigger chance", multiTurnRating.ExpectedProcValue == 2.25m);
        Check("Base value (PhysicalAttackBonus) is included alongside the proc value", multiTurnRating.BaseValue == 2m);

        var instantProc = Items.FlamingLongSword.Clone();
        instantProc.IsIdentified = true; // Fire: chance 1.0, magnitude 5, duration 0 (instant)
        var instantRating = ItemQualityCalculator.Evaluate(instantProc);
        Check("An instant proc (duration 0) is treated as a single application, not zero",
            instantRating.MaximumProcValue == 5m && instantRating.ExpectedProcValue == 5m);
    }

    private static void RegenerationUsesTheConfiguredHorizonUnlessADurationIsShorter()
    {
        var noExplicitDuration = new Item("Test Regen Ring", '=', "test", ItemType.Armor, EquipmentType.Ring,
            statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Regeneration, chance: 1.0, magnitude: 10) },
            isIdentified: true);
        Check("Regeneration with no explicit duration uses the configured horizon (10 HP/turn x 10 turns = 100)",
            ItemQualityCalculator.Evaluate(noExplicitDuration).UtilityValue == 100m);

        var shorterDuration = new Item("Test Regen Ring Short", '=', "test", ItemType.Armor, EquipmentType.Ring,
            statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Regeneration, chance: 1.0, magnitude: 10, duration: 5) },
            isIdentified: true);
        Check("Regeneration with a shorter explicit duration uses that duration instead of the full horizon (10 HP/turn x 5 turns = 50)",
            ItemQualityCalculator.Evaluate(shorterDuration).UtilityValue == 50m);
    }

    private static void NegativeModifiersAndCurseFlatPenaltySubtractFromQuality()
    {
        var cursedRing = Items.CursedRing.Clone();
        cursedRing.IsIdentified = true; // Agility -1 (x2.0 weight = -2), EncumbranceModifier -20 (x0.1 weight = -2), cursed (-5 flat)
        var rating = ItemQualityCalculator.Evaluate(cursedRing);

        Check("A negative stat modifier and harmful encumbrance both subtract from utility value", rating.UtilityValue == -4m);
        Check("A cursed item's flat penalty is applied on top of its own negative stats", rating.PenaltyValue == -5m);
        Check("Overall quality reflects every negative contribution combined", rating.OverallQuality == -9m);
    }

    private static void UnidentifiedItemRatingIsIncompleteWithOnlyBaseValueKnown()
    {
        var unidentified = Items.VenomfangDagger.Clone(); // unidentified by default -- PhysicalAttackBonus 2, hidden Poison proc
        var rating = ItemQualityCalculator.Evaluate(unidentified);

        Check("An unidentified item's rating is marked incomplete", !rating.IsComplete);
        Check("An unidentified item's base value is still known", rating.BaseValue == 2m);
        Check("An unidentified item's proc/utility/penalty values are all zero -- never a peek at concealed properties",
            rating.ExpectedProcValue == 0m && rating.MaximumProcValue == 0m && rating.UtilityValue == 0m && rating.PenaltyValue == 0m);
        Check("An unidentified item's overall quality equals just its base value", rating.OverallQuality == 2m);
    }

    private static void BothCompleteRatingsDeclareADefinitiveWinnerOrATie()
    {
        var sword = Items.ShortSword.Clone();
        sword.IsIdentified = true; // quality 4
        var mace = Items.Warhammer.Clone();
        mace.IsIdentified = true; // quality 11
        var result = ItemComparer.Compare(sword, ItemQualityCalculator.Evaluate(sword), mace, ItemQualityCalculator.Evaluate(mace));
        Check("Both complete with a real difference declares the higher-quality item the definitive winner",
            result.BothComplete && result.DefinitiveWinner == mace && result.Delta == 7m);

        var sword2 = Items.ShortSword.Clone();
        sword2.IsIdentified = true;
        var tie = ItemComparer.Compare(sword, ItemQualityCalculator.Evaluate(sword), sword2, ItemQualityCalculator.Evaluate(sword2));
        Check("Both complete and equal reports no definitive winner (comparable, not a win)",
            tie.BothComplete && tie.DefinitiveWinner == null);
    }

    private static void ItemComparerNeverDeclaresADefinitiveWinnerWhenEitherRatingIsIncomplete()
    {
        var identifiedSword = Items.ShortSword.Clone();
        identifiedSword.IsIdentified = true; // quality 4
        var unidentifiedDagger = Items.Dagger.Clone();
        unidentifiedDagger.IsIdentified = false; // base 2, incomplete

        var result = ItemComparer.Compare(identifiedSword, ItemQualityCalculator.Evaluate(identifiedSword),
            unidentifiedDagger, ItemQualityCalculator.Evaluate(unidentifiedDagger));
        Check("Either rating incomplete never declares a definitive winner", !result.BothComplete && result.DefinitiveWinner == null);
        Check("Either rating incomplete still hints the higher partial total as apparently stronger", result.ApparentlyStronger == identifiedSword);

        var equalButUnidentified = Items.VenomfangDagger.Clone(); // base 2, unidentified by default
        var equalAndIdentified = Items.Dagger.Clone();
        equalAndIdentified.IsIdentified = true; // base 2, identified
        var tieResult = ItemComparer.Compare(equalAndIdentified, ItemQualityCalculator.Evaluate(equalAndIdentified),
            equalButUnidentified, ItemQualityCalculator.Evaluate(equalButUnidentified));
        Check("Equal partial totals with an incomplete side hints no direction either",
            tieResult.ApparentlyStronger == null && tieResult.DefinitiveWinner == null);
    }

    private static void ProcDependentFlagReflectsWhetherMaximumProcWouldChangeTheWinner()
    {
        var steadyBase = new Item("Test Steady Blade", '/', "test", ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon,
            physicalAttackBonus: 10, weaponType: WeaponType.Sword, itemSize: ItemSize.Medium, size: Size.Medium, isIdentified: true);
        var riskyProc = new Item("Test Risky Blade", '/', "test", ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon,
            physicalAttackBonus: 6, weaponType: WeaponType.Sword, itemSize: ItemSize.Medium, size: Size.Medium, isIdentified: true,
            statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Fire, chance: 0.1, magnitude: 20, duration: 1) });
        // Expected: 6 + (20*1*0.1) = 8 < 10, steady wins realistically. Maximum: 6+20 = 26 > 10 -- the winner would flip if the proc always landed.
        var flips = ItemComparer.Compare(steadyBase, ItemQualityCalculator.Evaluate(steadyBase), riskyProc, ItemQualityCalculator.Evaluate(riskyProc));
        Check("A low-chance, high-magnitude proc that would flip the winner if it always landed is flagged proc-dependent",
            flips.DefinitiveWinner == steadyBase && flips.ProcDependent);

        var overwhelmingBase = new Item("Test Overwhelming Blade", '/', "test", ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon,
            physicalAttackBonus: 100, weaponType: WeaponType.Sword, itemSize: ItemSize.Medium, size: Size.Medium, isIdentified: true);
        var harmlessProc = new Item("Test Harmless Blade", '/', "test", ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon,
            physicalAttackBonus: 6, weaponType: WeaponType.Sword, itemSize: ItemSize.Medium, size: Size.Medium, isIdentified: true,
            statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Fire, chance: 0.1, magnitude: 20, duration: 1) });
        var steady = ItemComparer.Compare(overwhelmingBase, ItemQualityCalculator.Evaluate(overwhelmingBase), harmlessProc, ItemQualityCalculator.Evaluate(harmlessProc));
        Check("A proc too small to ever change the outcome is not flagged proc-dependent",
            steady.DefinitiveWinner == overwhelmingBase && !steady.ProcDependent);
    }

    private static void FindsAWeaponBehindAShieldAndNeverSelectsTheShieldItself()
    {
        var rng = new Random(804);
        var player = new Player("BehindShieldTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var shield = Items.Shield.Clone();
        shield.IsIdentified = true;
        var sword = Items.ShortSword.Clone();
        sword.IsIdentified = true;
        player.EquipInSlot(EquipmentSlot.PrimaryHand, shield);
        player.EquipInSlot(EquipmentSlot.OffHand, sword);

        var mace = Items.Warhammer.Clone();
        mace.IsIdentified = true;
        var comparable = EquippedComparisonTargetFinder.FindComparableEquipped(player, mace);
        Check("Only the Off-hand sword is found as a comparable equipped item, never the Primary Hand shield",
            comparable.Count == 1 && comparable[0].Slot == EquipmentSlot.OffHand && comparable[0].Item == sword);

        var target = EquippedComparisonTargetFinder.FindWeakestLegalReplacementTarget(player, mace);
        Check("The weakest legal replacement target is the Off-hand sword, correctly located and named",
            target != null && target.Value.Slot == EquipmentSlot.OffHand && target.Value.Item == sword);
    }

    private static void DualWieldSelectsTheWeakerOfTwoIdentifiedEquippedWeapons()
    {
        var rng = new Random(805);
        var player = new Player("DualWieldTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        player.KnownSkills.Add(SkillCatalog.DualWield);
        var mace = Items.Warhammer.Clone();
        mace.IsIdentified = true; // quality 11
        var dagger = Items.Dagger.Clone();
        dagger.IsIdentified = true; // quality 2 -- the weaker of the two
        player.EquipInSlot(EquipmentSlot.PrimaryHand, mace);
        player.EquipInSlot(EquipmentSlot.OffHand, dagger);

        var candidate = Items.ShortSword.Clone();
        candidate.IsIdentified = true; // quality 4 -- better than the dagger, worse than the mace
        var target = EquippedComparisonTargetFinder.FindWeakestLegalReplacementTarget(player, candidate);
        Check("Dual Wield selects the weaker of the two equipped weapons (the dagger, not the mace) as the replacement target",
            target != null && target.Value.Item == dagger && target.Value.Slot == EquipmentSlot.OffHand);
    }

    private static void CursedOrUnidentifiedEquippedItemsAreNeverSelectedAsAReplacementTarget()
    {
        var rng = new Random(806);
        var player = new Player("CurseExclusionTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var cursedDagger = Items.CursedDagger.Clone();
        cursedDagger.IsIdentified = true; // identified but cursed -- would otherwise look like an easy "weakest" pick
        player.EquipInSlot(EquipmentSlot.PrimaryHand, cursedDagger);

        var candidate = Items.ShortSword.Clone();
        candidate.IsIdentified = true;
        Check("A cursed equipped item is never returned as a legal replacement target, even though it looks weakest",
            EquippedComparisonTargetFinder.FindWeakestLegalReplacementTarget(player, candidate) == null);

        var rng2 = new Random(807);
        var player2 = new Player("UnidExclusionTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng2));
        player2.KnownSkills.Add(SkillCatalog.DualWield);
        var unidentifiedDagger = Items.VenomfangDagger.Clone(); // unidentified, apparent quality only 2
        var identifiedMace = Items.Warhammer.Clone();
        identifiedMace.IsIdentified = true; // quality 11
        player2.EquipInSlot(EquipmentSlot.PrimaryHand, unidentifiedDagger);
        player2.EquipInSlot(EquipmentSlot.OffHand, identifiedMace);

        var candidate2 = Items.ShortSword.Clone();
        candidate2.IsIdentified = true;
        var target2 = EquippedComparisonTargetFinder.FindWeakestLegalReplacementTarget(player2, candidate2);
        Check("An unidentified equipped item is never auto-selected as the weakest target, even though its apparent quality looks lowest",
            target2 != null && target2.Value.Item == identifiedMace);
    }

    private static void CanReplaceInContextEnforcesFullEquipLegality()
    {
        var rng = new Random(808);
        var mage = new Player("LegalityTester", CharacterClass.Mage, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Mage, rng));
        var legalDagger = Items.Dagger.Clone();
        legalDagger.IsIdentified = true;
        mage.EquipInSlot(EquipmentSlot.PrimaryHand, legalDagger);

        bool mageAllowsBlunt = mage.Class.AllowedWeaponTypes.Contains(WeaponType.Blunt);
        Check("Test setup: this weapon type is not allowed for the test class (otherwise the test proves nothing)", !mageAllowsBlunt);

        var restrictedMace = new Item("Test Restricted Mace", ')', "test", ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon,
            physicalAttackBonus: 50, weaponType: WeaponType.Blunt, itemSize: ItemSize.Medium, size: Size.Medium, isIdentified: true);

        Check("A class-illegal candidate can never replace an equipped item, no matter how much higher its apparent quality",
            !ItemComparisonCompatibility.CanReplaceInContext(mage, restrictedMace, legalDagger, EquipmentSlot.PrimaryHand));

        var gameLoop = new GameLoop(mage);
        Check("The same illegal candidate produces no pickup upgrade offer at all",
            gameLoop.DecidePickupUpgradeOffer(restrictedMace) == null);
    }

    private static void UnidentifiedPickupNeverOffersToEquipButHintsWhenACounterpartExists()
    {
        var rng = new Random(809);
        var player = new Player("UnidHintTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        level.GroundItems.RemoveAll(g => g.X == player.X && g.Y == player.Y);

        var equippedSword = Items.ShortSword.Clone();
        equippedSword.IsIdentified = true;
        player.EquipInSlot(EquipmentSlot.PrimaryHand, equippedSword); // Off-hand stays empty -- an eligible empty slot exists

        var unidentifiedLongSword = Items.BlessedLongSword.Clone(); // isIdentified: false by default
        level.AddItem(player.X, player.Y, unidentifiedLongSword);

        // Must never block on Console.ReadKey -- if this hangs, the suppression didn't take.
        bool pickedUp = gameLoop.HandlePickUp(level);

        Check("An unidentified pickup with an empty compatible slot is still added to inventory",
            pickedUp && player.Inventory.Items.Contains(unidentifiedLongSword));
        Check("An unidentified pickup never auto-equips into an empty slot -- deliberately changed behavior",
            player.Equipment.Get(EquipmentSlot.OffHand) == null);
        Check("An unidentified pickup with a comparable equipped counterpart shows the identify-to-compare hint, naming the real counterpart",
            gameLoop.StatusMessages[^1].Contains("Identify it") && gameLoop.StatusMessages[^1].Contains(equippedSword.DisplayName));
    }

    private static void UnidentifiedPickupWithNoComparableCounterpartShowsNoExtraHint()
    {
        var rng = new Random(810);
        var player = new Player("UnidNoHintTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        level.GroundItems.RemoveAll(g => g.X == player.X && g.Y == player.Y);

        var unidentifiedDagger = Items.VenomfangDagger.Clone();
        level.AddItem(player.X, player.Y, unidentifiedDagger);

        bool pickedUp = gameLoop.HandlePickUp(level);

        Check("An unidentified pickup with nothing comparable equipped shows only the plain pickup message",
            pickedUp && !gameLoop.StatusMessages[^1].Contains("Identify it") && gameLoop.StatusMessages[^1].Contains("pick up"));
    }

    private static void UnidentifiedPickupNeverOffersReplacementEvenWithAFullLoadout()
    {
        var rng = new Random(811);
        var player = new Player("UnidFullLoadoutTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        player.KnownSkills.Add(SkillCatalog.DualWield);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        level.GroundItems.RemoveAll(g => g.X == player.X && g.Y == player.Y);

        var mace = Items.Warhammer.Clone();
        mace.IsIdentified = true;
        var dagger = Items.Dagger.Clone();
        dagger.IsIdentified = true;
        player.EquipInSlot(EquipmentSlot.PrimaryHand, mace);
        player.EquipInSlot(EquipmentSlot.OffHand, dagger);

        // A synthetic weapon with an overwhelming physicalAttackBonus -- would be an obvious
        // upgrade over either equipped weapon if identified, but must never be offered while not.
        var superWeapon = new Item("Test Excalibur", '/', "test", ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon,
            physicalAttackBonus: 50, weaponType: WeaponType.Sword, itemSize: ItemSize.Medium, size: Size.Medium, isIdentified: false);
        level.AddItem(player.X, player.Y, superWeapon);

        bool pickedUp = gameLoop.HandlePickUp(level);

        Check("Neither equipped weapon is replaced by an unidentified item, no matter how strong it would be if identified",
            pickedUp && player.Equipment.Get(EquipmentSlot.PrimaryHand) == mace && player.Equipment.Get(EquipmentSlot.OffHand) == dagger);
        Check("The hint sentence still names a real equipped counterpart instead of silently doing nothing",
            gameLoop.StatusMessages[^1].Contains("Identify it"));
    }

    private static void GenuineIdentifiedUpgradeIsFoundBehindAFullLoadout()
    {
        var rng = new Random(812);
        var player = new Player("UpgradeFoundTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var shield = Items.Shield.Clone();
        shield.IsIdentified = true;
        var sword = Items.ShortSword.Clone();
        sword.IsIdentified = true;
        player.EquipInSlot(EquipmentSlot.PrimaryHand, shield);
        player.EquipInSlot(EquipmentSlot.OffHand, sword);
        var gameLoop = new GameLoop(player);

        var mace = Items.Warhammer.Clone();
        mace.IsIdentified = true;
        var target = gameLoop.DecidePickupUpgradeOffer(mace);

        Check("A genuine, legal, identified upgrade is found, correctly targeting the Off-hand sword and skipping the Primary Hand shield",
            target != null && target.Value.Slot == EquipmentSlot.OffHand && target.Value.Item == sword);
    }

    private static void TiedOrWorsePickupsProduceNoUpgradeOffer()
    {
        var rng = new Random(813);
        var player = new Player("NoUpgradeTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var equippedSword = Items.ShortSword.Clone();
        equippedSword.IsIdentified = true;
        player.EquipInSlot(EquipmentSlot.PrimaryHand, equippedSword);
        var gameLoop = new GameLoop(player);

        var tiedSword = Items.ShortSword.Clone();
        tiedSword.IsIdentified = true;
        Check("An exactly-tied candidate produces no upgrade offer", gameLoop.DecidePickupUpgradeOffer(tiedSword) == null);

        var worseDagger = Items.Dagger.Clone();
        worseDagger.IsIdentified = true;
        Check("A strictly worse candidate produces no upgrade offer", gameLoop.DecidePickupUpgradeOffer(worseDagger) == null);
    }

    private static void GetAllNeverTriggersPickupUpgradeReplacement()
    {
        var rng = new Random(814);
        var player = new Player("GetAllUpgradeTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var shield = Items.Shield.Clone();
        shield.IsIdentified = true;
        var sword = Items.ShortSword.Clone();
        sword.IsIdentified = true;
        player.EquipInSlot(EquipmentSlot.PrimaryHand, shield);
        player.EquipInSlot(EquipmentSlot.OffHand, sword);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        level.GroundItems.RemoveAll(g => g.X == player.X && g.Y == player.Y);

        var mace = Items.Warhammer.Clone();
        mace.IsIdentified = true; // a genuine upgrade over the Off-hand sword
        level.AddItem(player.X, player.Y, mace);

        bool pickedUp = gameLoop.PickUpAll(level, level.GetItemsAt(player.X, player.Y));

        Check("Get All picks up a genuine upgrade into inventory", pickedUp && player.Inventory.Items.Contains(mace));
        Check("Get All never replaces equipped gear either, even with a genuine upgrade available",
            player.Equipment.Get(EquipmentSlot.OffHand) == sword);
    }

    private static void ExamineShowsTheLightweightComparisonOnlyWhenApplicable()
    {
        var rng = new Random(815);
        var player = new Player("ExamineTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var identifiedSword = Items.ShortSword.Clone();
        identifiedSword.IsIdentified = true;

        string noCounterpart = InventoryScreen.FormatItemDetails(player, identifiedSword);
        Check("Examining an identified item with no comparable equipped counterpart shows no extra comparison line",
            !noCounterpart.Contains("Quality"));

        var equippedSword = Items.ShortSword.Clone();
        equippedSword.IsIdentified = true;
        player.EquipInSlot(EquipmentSlot.PrimaryHand, equippedSword);
        var mace = Items.Warhammer.Clone();
        mace.IsIdentified = true;
        string withCounterpart = InventoryScreen.FormatItemDetails(player, mace);
        Check("Examining an identified item with a comparable equipped counterpart shows the definite comparison line",
            withCounterpart.Contains("Quality:") && withCounterpart.Contains("Difference: +7"));

        var unidentifiedDagger = Items.VenomfangDagger.Clone();
        string hedged = InventoryScreen.FormatItemDetails(player, unidentifiedDagger);
        Check("Examining an unidentified item against an identified counterpart uses hedged wording naming the actually-stronger side, never a definite difference",
            hedged.Contains("Equipped Short Sword appears stronger") && !hedged.Contains("Difference:"));
    }

    private static void MultipleUnidentifiedEquippedCounterpartsSuppressTheLightweightLine()
    {
        var rng = new Random(816);
        var player = new Player("AmbiguousCounterpartTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        player.KnownSkills.Add(SkillCatalog.DualWield);
        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.VenomfangDagger.Clone()); // unidentified
        player.EquipInSlot(EquipmentSlot.OffHand, Items.CursedDagger.Clone()); // unidentified

        var identifiedSword = Items.ShortSword.Clone();
        identifiedSword.IsIdentified = true;
        string details = InventoryScreen.FormatItemDetails(player, identifiedSword);

        Check("With multiple equipped counterparts and none identified, no lightweight comparison line is guessed",
            !details.Contains("Quality") && !details.Contains("Visible quality"));
    }

    private static void OwnedItemLabelAndTraderPriceLineFormatCorrectly()
    {
        var rng = new Random(817);
        var player = new Player("LabelTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var equippedSword = Items.ShortSword.Clone();
        equippedSword.IsIdentified = true;
        player.EquipInSlot(EquipmentSlot.OffHand, equippedSword);
        var inventoryDagger = Items.Dagger.Clone();
        inventoryDagger.IsIdentified = true;
        player.Inventory.AddItem(inventoryDagger);

        Check("An equipped item's label names its real slot",
            ItemComparisonFormatter.OwnedItemLabel(player, equippedSword).Contains("equipped: Off-hand"));
        Check("An inventory item's label says inventory",
            ItemComparisonFormatter.OwnedItemLabel(player, inventoryDagger).EndsWith("-- inventory"));

        var ratingA = ItemQualityCalculator.Evaluate(equippedSword);
        var ratingB = ItemQualityCalculator.Evaluate(inventoryDagger);
        var table = ItemComparisonFormatter.FormatTable("A", ratingA, "B", ratingB, priceLineB: "42g");
        Check("A supplied trader price line is included in the rendered table", table.Any(line => line.Contains("42g")));
    }

    private static void FormatDeltaSentenceWordingCoversAllFourResultShapes()
    {
        var itemA = Items.ShortSword.Clone();
        var itemB = Items.Warhammer.Clone();

        var win = new ComparisonResult { BothComplete = true, DefinitiveWinner = itemB, ApparentlyStronger = itemB, Delta = 7m };
        Check("Both complete with a winner uses definite '+N quality' wording",
            ItemComparisonFormatter.FormatDeltaSentence(("A", itemA), ("B", itemB), win).Contains("has +7 quality"));

        var tie = new ComparisonResult { BothComplete = true, DefinitiveWinner = null, ApparentlyStronger = null, Delta = 0m };
        Check("Both complete and tied uses 'comparable' wording, never declaring a winner",
            ItemComparisonFormatter.FormatDeltaSentence(("A", itemA), ("B", itemB), tie) == "A and B are comparable.");

        var hedged = new ComparisonResult { BothComplete = false, DefinitiveWinner = null, ApparentlyStronger = itemA, Delta = 2m };
        Check("Either incomplete uses hedged 'appears stronger' wording, never a definite winner",
            ItemComparisonFormatter.FormatDeltaSentence(("A", itemA), ("B", itemB), hedged).Contains("appears stronger based on known properties"));

        var undetermined = new ComparisonResult { BothComplete = false, DefinitiveWinner = null, ApparentlyStronger = null, Delta = 0m };
        Check("Either incomplete with equal partial totals says it cannot be fully evaluated",
            ItemComparisonFormatter.FormatDeltaSentence(("A", itemA), ("B", itemB), undetermined) == "Cannot be fully evaluated until identified.");
    }

    /// <summary>
    /// Regression check for a real bug report: a monster carrying an unidentified item (e.g.
    /// Venomfang Dagger, isIdentified: false in Items.cs) revealed its true name in both the
    /// death message ("It drops a Venomfang Dagger.") and the pickup message ("You pick up a
    /// Venomfang Dagger."), even though the very same item then showed up in the inventory as
    /// "Unidentified Weapon" -- both messages were built from Item.Name directly instead of
    /// Item.DisplayName. AwardDeathRewards/HandlePickUp were bumped to internal specifically
    /// for this check.
    /// </summary>
    private static void DeathAndPickupMessagesNeverLeakAnUnidentifiedItemsName()
    {
        var rng = new Random(16);
        var player = new Player("LeakTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        // GameLoop's own dungeon generation isn't seeded from this test's rng (it uses its own
        // unseeded Random internally), so it can occasionally, randomly drop a generated item
        // right on the player's spawn tile -- guarantee a clean tile before this test's own
        // pickup logic runs, or that stray item plus this one would make HandlePickUp think
        // there are 2 items here and route through the interactive multi-item menu instead of
        // auto-picking up the one this test actually cares about.
        level.GroundItems.RemoveAll(drop => drop.X == player.X && drop.Y == player.Y);

        var monster = Monster.CreateRandom(player.X, player.Y, 1, rng);
        var unidentifiedWeapon = Items.VenomfangDagger.Clone();
        var slot = monster.Equipment.FindAutoEquipSlot(unidentifiedWeapon);
        monster.Equipment.EquipInSlot(slot!.Value, unidentifiedWeapon);
        monster.Health.TakeDamage(monster.Health.Max);
        level.Actors.Add(monster);

        gameLoop.AwardDeathRewards(level);
        string deathMessage = gameLoop.StatusMessages[^1];
        Check("A death message never reveals an unidentified dropped item's true name",
            !deathMessage.Contains(Items.VenomfangDagger.Name));

        // Corpse System: the dagger is no longer a loose ground item -- it's inside the corpse
        // the monster left behind, so the "does the shown name leak the true identity" check now
        // applies to the item as it would be displayed from that corpse's own contents.
        var corpse = level.GetCorpsesAt(monster.X, monster.Y).FirstOrDefault();
        var containedWeapon = corpse?.Container.Contents.Items.FirstOrDefault(i => i.Type == ItemType.Weapon);
        Check("The dropped weapon actually ended up inside the corpse", containedWeapon != null);
        Check("The item's own display name (as shown once the corpse is opened) never reveals its true name while unidentified",
            containedWeapon != null && !containedWeapon.DisplayName.Contains(Items.VenomfangDagger.Name) && containedWeapon.DisplayName.Contains("Unidentified"));
    }

    // --- Starting gear identification, Look naming its target -------------------------

    /// <summary>
    /// Regression check for a real bug: a Thief's starting-armor roll could land on
    /// Ring of Regeneration (ItemType.Armor, isIdentified: false in Items.cs) and show up
    /// as "Unidentified Ring" the moment the game started -- see StartingGearGenerator.Equip.
    /// Rolls starting gear across every class/race pairing many times over, since which
    /// item (if any) lands on an unidentified catalog entry is random.
    /// </summary>
    private static void StartingGearIsAlwaysIdentified()
    {
        var rng = new Random(13);
        bool everFoundUnidentifiedCandidate = false;
        bool allEquippedAreIdentified = true;
        bool noneEquippedAreCursed = true;

        foreach (var characterClass in CharacterClass.All)
        {
            foreach (var race in Race.All)
            {
                for (int i = 0; i < 20; i++)
                {
                    var player = new Player("GearTester", characterClass, race, CharacterStats.Roll(race, characterClass, rng));
                    StartingGearGenerator.EquipStartingGear(player, rng);

                    foreach (var equipped in player.Equipment.AllEquipped.Select(kvp => kvp.Value))
                    {
                        if (!equipped.IsIdentified)
                        {
                            allEquippedAreIdentified = false;
                        }
                        if (equipped.IsCursed)
                        {
                            noneEquippedAreCursed = false;
                        }
                    }
                }
            }
        }

        // Also confirms Items.All still actually contains an unidentified-by-default
        // candidate eligible as starting armor -- otherwise this check would trivially
        // pass without ever having exercised the fix.
        everFoundUnidentifiedCandidate = Items.All.Any(i => i.Type == ItemType.Armor && !i.IsIdentified);
        // Same trivial-pass guard, for the cursed-item exclusion below.
        bool catalogHasACursedCandidate = Items.All.Any(i => (i.Type == ItemType.Weapon || i.Type == ItemType.Wand || i.Type == ItemType.Armor) && i.IsCursed);

        Check("Starting gear is always identified, even when the roll lands on a normally-unidentified item",
            everFoundUnidentifiedCandidate && allEquippedAreIdentified);
        Check("Starting gear never includes a cursed item, even though the catalog has cursed candidates",
            catalogHasACursedCandidate && noneEquippedAreCursed);
    }

    /// <summary>
    /// Regression check: rolling a launcher (Bow/Crossbow/Sling) as the starting weapon used to
    /// leave the Ammunition slot empty -- StartingGearGenerator only ever picked from
    /// ItemType.Weapon/Wand, and never granted a companion ammo item, so a fresh character could
    /// open the game with a Bow and nothing to fire from it. Rolls starting gear across every
    /// class/race pairing many times over, since which weapon (if any) lands on a launcher is
    /// random.
    /// </summary>
    private static void StartingGearLauncherAlwaysComesWithMatchingAmmo()
    {
        var rng = new Random(17);
        bool everRolledALauncher = false;
        bool everyLauncherHasMatchingAmmo = true;

        foreach (var characterClass in CharacterClass.All)
        {
            foreach (var race in Race.All)
            {
                for (int i = 0; i < 20; i++)
                {
                    var player = new Player("LauncherGearTester", characterClass, race, CharacterStats.Roll(race, characterClass, rng));
                    var gear = StartingGearGenerator.EquipStartingGear(player, rng);

                    if (gear.Weapon == null || !gear.Weapon.RequiredAmmunitionType.HasValue)
                    {
                        continue;
                    }

                    everRolledALauncher = true;
                    var ammo = player.Equipment.Get(EquipmentSlot.Ammunition);
                    if (ammo == null || ammo.AmmunitionType != gear.Weapon.RequiredAmmunitionType)
                    {
                        everyLauncherHasMatchingAmmo = false;
                    }
                }
            }
        }

        Check("At least one seed actually rolled a launcher as the starting weapon", everRolledALauncher);
        Check("Every starting launcher comes with a matching ammo type already equipped in Ammunition",
            everyLauncherHasMatchingAmmo);
    }

    // --- Luck (Revised Luck proposal): rolled attribute, inert, +0 for every class -----------

    private static void LuckRollsWithinEachRacesOwnRangeAcrossEveryClass()
    {
        var rng = new Random(19);
        bool everyRollWithinRange = true;

        foreach (var race in Race.All)
        {
            var range = race.StatRanges[PrimaryAttribute.Luck];
            foreach (var characterClass in CharacterClass.All)
            {
                for (int i = 0; i < 30; i++)
                {
                    var stats = CharacterStats.Roll(race, characterClass, rng);
                    int luck = stats.Get(PrimaryAttribute.Luck).BaseValue;
                    if (luck < range.Min || luck > range.Max)
                    {
                        everyRollWithinRange = false;
                    }
                }
            }
        }

        Check("Luck always rolls within the character's own race's Luck range, for every class",
            everyRollWithinRange);
    }

    private static void HalflingHasTheHighestAverageLuckAndDwarfTheLowest()
    {
        // Confirms the proposal's own racial design intent (Halfling: distinctly luckiest;
        // Dwarf: rarely favored by chance) via each range's average -- individual rolls can
        // still overlap across races (an unlucky Halfling can roll below a lucky Human), so this
        // checks the documented averages (Human 11, Dwarf 9.5, Elf 11.5, Halfling 13.5), not that
        // the ranges themselves never overlap.
        double Average(Race r) => (r.StatRanges[PrimaryAttribute.Luck].Min + r.StatRanges[PrimaryAttribute.Luck].Max) / 2.0;

        Check("Halfling has the strictly highest average Luck of any race",
            Race.All.Where(r => r != Race.Halfling).All(r => Average(Race.Halfling) > Average(r)));
        Check("Dwarf has the strictly lowest average Luck of any race",
            Race.All.Where(r => r != Race.Dwarf).All(r => Average(Race.Dwarf) < Average(r)));
    }

    private static void EveryClassHasAnExplicitZeroLuckModifier()
    {
        Check("Every class's StatModifiers dictionary explicitly lists a +0 Luck entry",
            CharacterClass.All.All(c => c.StatModifiers.ContainsKey(PrimaryAttribute.Luck) && c.StatModifiers[PrimaryAttribute.Luck] == 0));
    }

    /// <summary>Builds a minimal SaveData for ToPlayer, same idiom as OlderSaveDataWithoutAdventureRecordFieldStillLoadsWithAZeroedRecord -- no real file I/O, just the DTO SaveManager.ToPlayer actually consumes. `luckEntry` is omitted entirely (not just zeroed) when null, simulating a save genuinely written before Luck existed.</summary>
    private static SaveData BuildMinimalSaveData(CharacterClass characterClass, Race race, int? luckEntry)
    {
        var statsData = new Dictionary<PrimaryAttribute, StatBlockData>
        {
            [PrimaryAttribute.Strength] = new StatBlockData { BaseValue = 10 },
            [PrimaryAttribute.Constitution] = new StatBlockData { BaseValue = 10 },
            [PrimaryAttribute.Agility] = new StatBlockData { BaseValue = 10 },
            [PrimaryAttribute.Wisdom] = new StatBlockData { BaseValue = 10 },
            [PrimaryAttribute.Knowledge] = new StatBlockData { BaseValue = 10 },
            [PrimaryAttribute.Charisma] = new StatBlockData { BaseValue = 10 }
        };
        if (luckEntry.HasValue)
        {
            statsData[PrimaryAttribute.Luck] = new StatBlockData { BaseValue = luckEntry.Value };
        }

        return new SaveData
        {
            ClassName = characterClass.Name,
            RaceName = race.Name,
            Stats = statsData
        };
    }

    private static void LuckSurvivesASaveLoadRoundTrip()
    {
        var data = BuildMinimalSaveData(CharacterClass.Thief, Race.Halfling, luckEntry: 16);
        var restored = SaveManager.ToPlayer(data);

        Check("Luck round-trips through save data exactly, same as every other primary attribute",
            restored.Stats.Adjusted(PrimaryAttribute.Luck) == 16);
    }

    /// <summary>
    /// Regression guard for the migration rule: an old save has no PrimaryAttribute.Luck key at
    /// all in its serialized Stats dictionary, which must NOT resolve to the lazy-default LCK: 0 --
    /// see SaveManager.LoadStats's own doc comment for the fixed-racial-midpoint rule and why the
    /// exact values below (10, 12) are what (min+max+1)/2 produces for Dwarf/Elf's half-integer
    /// true midpoints.
    /// </summary>
    private static void OldSaveDataMissingLuckMigratesToAFixedRacialMidpoint()
    {
        var expectedMidpoints = new Dictionary<Race, int>
        {
            [Race.Human] = 11,
            [Race.Dwarf] = 10,
            [Race.Elf] = 12,
            [Race.Halfling] = 14
        };

        bool everyMidpointCorrect = true;
        foreach (var (race, expectedLuck) in expectedMidpoints)
        {
            var data = BuildMinimalSaveData(CharacterClass.Warrior, race, luckEntry: null);
            var restored = SaveManager.ToPlayer(data);

            if (restored.Stats.Adjusted(PrimaryAttribute.Luck) != expectedLuck)
            {
                everyMidpointCorrect = false;
            }
        }

        Check("A save missing Luck data migrates to the exact fixed racial midpoint for every race",
            everyMidpointCorrect);
    }

    private static void LookNamesTheTarget()
    {
        var rng = new Random(14);
        var player = new Player("LookTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var monster = Monster.CreateRandom(0, 0, difficultyLevel: 1, rng);

        string description = LookService.DescribeCharacter(player, monster);

        Check("Looking at a monster names it, not just its flavor description",
            description.StartsWith(char.ToUpper(monster.Name[0]) + monster.Name[1..] + ": "));
    }

    /// <summary>A boss's DisplayName is a proper name that no longer reveals its creature type (see the Boss Monster Naming doc), so Look additionally shows the underlying creature type in parentheses -- otherwise the "...than others of its kind" flavor clause CreateBoss appends has no antecedent.</summary>
    private static void LookOnABossShowsCreatureTypeAlongsideItsIdentity()
    {
        var rng = new Random(14);
        var player = new Player("LookTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var boss = Monster.CreateBoss(0, 0, difficultyLevel: 1, rng);

        string description = LookService.DescribeCharacter(player, boss);
        string expectedPrefix = $"{char.ToUpper(boss.DisplayName[0]) + boss.DisplayName[1..]} ({char.ToUpper(boss.Name[0]) + boss.Name[1..]}): ";

        Check("Looking at a boss shows its proper name plus its creature type in parentheses", description.StartsWith(expectedPrefix));
    }

    /// <summary>
    /// Regression check for a real hang: on a deep floor with many freshly-spawned monsters
    /// (all starting at 0 energy, so many cross the scheduler's ready threshold in the same
    /// synchronized batch -- see TurnScheduler), GameLoop.Run used to call Renderer.Render
    /// unconditionally for every single one of their turns before the player ever got control
    /// back. Dozens of full-screen console writes stacking up back-to-back was slow enough to
    /// look and feel like the game had frozen. See GameLoop.ShouldRenderTurn.
    /// </summary>
    private static void RenderIsSkippedForOffScreenMonsters()
    {
        var rng = new Random(15);
        var level = new Level(1, 10, 10);
        var player = new Player("RenderTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));

        var visibleMonster = Monster.CreateRandom(1, 1, 1, rng);
        var offScreenMonster = Monster.CreateRandom(2, 2, 1, rng);
        level.Tiles[1, 1].IsVisible = true;
        level.Tiles[2, 2].IsVisible = false;

        Check("The player's own turn always renders", GameLoop.ShouldRenderTurn(level, player));
        Check("A monster's turn renders when it's on a currently-visible tile", GameLoop.ShouldRenderTurn(level, visibleMonster));
        Check("A monster's turn is skipped when it's not on a currently-visible tile", !GameLoop.ShouldRenderTurn(level, offScreenMonster));
    }

    /// <summary>
    /// Two identical-seed calls -- CreateBoss's own internal CreateRandom call consumes rng
    /// draws in exactly the same sequence a standalone CreateRandom call would, so the
    /// pre-scaling monster is bit-for-bit identical and the boss multipliers/bonuses from
    /// BossConfig can be checked as exact deltas instead of approximate ranges.
    /// </summary>
    private static void BossHasScaledStatsAndGuaranteedEquipment()
    {
        var baseline = Monster.CreateRandom(0, 0, 5, new Random(20));
        var boss = Monster.CreateBoss(0, 0, 5, new Random(20));

        Check("CreateBoss sets IsBoss (and CreateRandom never does)", boss.IsBoss && !baseline.IsBoss);
        // >= rather than == on the next two: guaranteed boss equipment (EnsureBossEquipment) can equip into an
        // empty slot and add its own PhysicalAttackBonus/DefenseBonus on top of pure multiplier/flat-bonus scaling,
        // so an exact-formula match isn't guaranteed -- only that scaling was applied at all.
        Check("A boss's DefensePower is at least the baseline's plus BossDefenseBonus", boss.DefensePower >= baseline.DefensePower + BossConfig.BossDefenseBonus);
        Check("A boss's physical attack is at least the baseline's scaled by BossDamageMultiplier",
            boss.BasePhysicalAttackPower >= (int)Math.Round(baseline.BasePhysicalAttackPower * BossConfig.BossDamageMultiplier));
        Check("A boss's max HP is the baseline's scaled by BossHealthMultiplier",
            boss.Health.Max == Math.Max(1, (int)Math.Round(baseline.Health.Max * BossConfig.BossHealthMultiplier)));

        var bossItems = boss.Equipment.AllEquipped.Select(kvp => kvp.Value).Concat(boss.Inventory.Items);
        Check("A boss always carries at least one item with a special property",
            bossItems.Any(i => i.StatModifiers.Count > 0 || i.StatusEffects.Count > 0 || i.IsBlessed || i.IsCursed));
    }

    /// <summary>
    /// Boss Monster Naming -- Epithet System: a boss's underlying Name/CreatureType must
    /// stay exactly as a normal spawn would have them (every ordinary gameplay system keeps
    /// reading those unchanged), while its player-facing DisplayName is an independently
    /// generated proper name + epithet that never mentions the creature type at all.
    /// </summary>
    private static void BossDisplayNameIsIndependentOfCreatureType()
    {
        var baseline = Monster.CreateRandom(0, 0, 5, new Random(21));
        var boss = Monster.CreateBoss(0, 0, 5, new Random(21));

        Check("A boss's underlying Name is unchanged from what CreateRandom would have produced", boss.Name == baseline.Name);
        Check("A boss's DisplayName differs from its underlying Name", boss.DisplayName != boss.Name);
        Check("A non-boss's DisplayName is just its Name", baseline.DisplayName == baseline.Name);
        Check("A boss reports a proper-noun DisplayName; a non-boss does not", boss.UsesProperNounDisplayName && !baseline.UsesProperNounDisplayName);
        Check("A boss's BossName/BossEpithet are both populated", !string.IsNullOrEmpty(boss.BossName) && !string.IsNullOrEmpty(boss.BossEpithet));

        string expectedDisplayName = boss.BossEpithetFormat == EpithetFormat.The
            ? $"{boss.BossName} the {boss.BossEpithet}"
            : $"{boss.BossName} {boss.BossEpithet}";
        Check("DisplayName combines BossName/BossEpithet per BossEpithetFormat", boss.DisplayName == expectedDisplayName);

        Check("WithArticle never adds a/an before a boss's proper-noun DisplayName", CombatMessages.WithArticle(boss) == boss.DisplayName);
        Check("WithArticle still adds a/an before a non-boss's common-noun Name", CombatMessages.WithArticle(baseline) != baseline.DisplayName);
        Check("Label never adds \"The\" before a boss's proper-noun DisplayName", CombatMessages.Label(boss, capitalized: true) == boss.DisplayName);
        Check("Label still adds \"The\" before a non-boss's common-noun Name", CombatMessages.Label(baseline, capitalized: true) == $"The {baseline.DisplayName}");
    }

    /// <summary>
    /// End-to-end smoke test through the real (private) dungeon generator -- keeps rolling
    /// fresh seeds until one happens to produce a boss room (BossRoomSpawnChance is well
    /// under 100%, so this isn't guaranteed on the first try), then checks the floor-level
    /// invariants the design doc cares about: exactly one boss, a normal-sized entourage
    /// clustered near it, and a locked-but-always-bashable door guarding the approach.
    /// </summary>
    private static void DungeonGenerationCanProduceABossRoom()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            var rng = new Random(seed);
            var level = DungeonGenerator.Generate(floorIndex: 10, width: 60, height: 22, rng, difficultyLevel: 10);
            var monsters = level.Actors.OfType<Monster>().ToList();
            var bosses = monsters.Where(m => m.IsBoss).ToList();

            if (bosses.Count == 0)
            {
                continue;
            }

            var boss = bosses[0];
            int nearbyNonBossCount = monsters.Count(m => !m.IsBoss && Math.Max(Math.Abs(m.X - boss.X), Math.Abs(m.Y - boss.Y)) <= 6);
            bool hasLockedBashableDoorNearby = level.Doors.Any(d =>
                Math.Max(Math.Abs(d.X - boss.X), Math.Abs(d.Y - boss.Y)) <= 6 && d.IsLocked && d.IsBashable);
            bool bossNotOnStairs = (boss.X, boss.Y) != level.StairsUpPosition && (boss.X, boss.Y) != level.StairsDownPosition;

            Check("A generated floor never has more than one boss", bosses.Count == 1);
            Check("A boss room's entourage size falls within BossMinEntourage..BossMaxEntourage",
                nearbyNonBossCount >= BossConfig.BossMinEntourage && nearbyNonBossCount <= BossConfig.BossMaxEntourage);
            Check("A boss room has a locked, bashable door guarding it", hasLockedBashableDoorNearby);
            Check("A boss is never placed on the stairs", bossNotOnStairs);
            Check("A boss room's interior has no way out except through a door (no leaked second pathway)",
                BossRoomInteriorNeverReachesRestOfFloor(level, boss));
            return;
        }

        Skip("Boss room generation", "200 seeds rolled and none produced a boss room -- BossRoomSpawnChance or placement may be broken");
    }

    /// <summary>
    /// Regression check for the "areas that aren't a room but aren't a hallway either" bug:
    /// this generator should only ever produce two shapes of walkable area -- a room
    /// (anything inside a rectangle from the internal `roomRects` overload) or a hall
    /// (exactly 1 tile wide for its whole length). A leftover 2x2-all-walkable block outside
    /// every known room rectangle is exactly what "wider than a hallway, but not a real room"
    /// looks like on the tile grid; a hall tile with 3+ walkable orthogonal neighbors is a
    /// T/+ junction, which also isn't "a hallway" by the 1-wide definition.
    /// </summary>
    private static void DungeonGeneratesOnlyRoomsAndStrictOneWideHalls()
    {
        bool anyWideBlobFound = false;
        bool anyJunctionFound = false;
        var orthogonalOffsets = new[] { (0, -1), (0, 1), (-1, 0), (1, 0) };

        for (int seed = 0; seed < 30; seed++)
        {
            var rng = new Random(seed);
            var level = DungeonGenerator.Generate(floorIndex: 5, width: 60, height: 22, rng, difficultyLevel: 5, forceTraderSpawn: false, out var roomRects);

            bool IsInsideAnyRoom(int x, int y) =>
                roomRects.Any(r => x >= r.X && x < r.X + r.Width && y >= r.Y && y < r.Y + r.Height);

            for (int x = 0; x < level.Width - 1; x++)
            {
                for (int y = 0; y < level.Height - 1; y++)
                {
                    bool allFourWalkable = level.IsWalkable(x, y) && level.IsWalkable(x + 1, y)
                        && level.IsWalkable(x, y + 1) && level.IsWalkable(x + 1, y + 1);
                    if (allFourWalkable
                        && !IsInsideAnyRoom(x, y) && !IsInsideAnyRoom(x + 1, y)
                        && !IsInsideAnyRoom(x, y + 1) && !IsInsideAnyRoom(x + 1, y + 1))
                    {
                        anyWideBlobFound = true;
                    }
                }
            }

            for (int x = 0; x < level.Width; x++)
            {
                for (int y = 0; y < level.Height; y++)
                {
                    if (!level.IsWalkable(x, y) || IsInsideAnyRoom(x, y))
                    {
                        continue;
                    }
                    int openNeighbors = orthogonalOffsets.Count(o => level.IsWalkable(x + o.Item1, y + o.Item2));
                    if (openNeighbors > 2)
                    {
                        anyJunctionFound = true;
                    }
                }
            }
        }

        Check("Generated floors never contain a walkable area wider than a room or a proper 1-tile hall", !anyWideBlobFound);
        Check("Every hall tile has at most 2 walkable orthogonal neighbors (straight-through or a single bend, never a junction)", !anyJunctionFound);
    }

    /// <summary>
    /// Every room the generator placed (main chain + boss room, if any) must be reachable
    /// from the entrance once every door is treated as passable -- Level.IsWalkable already
    /// ignores Door state entirely (a Door is an overlay, not a Tile mutation), so a plain
    /// walkable-tile BFS from the entrance already models "doors are open." This guards
    /// against the stricter placement/connector validation added for the rooms-and-halls
    /// rewrite accidentally leaving a room stranded instead of just skipping it.
    /// </summary>
    private static void DungeonFloorIsFullyConnectedThroughDoors()
    {
        bool allSeedsFullyConnected = true;
        var orthogonalOffsets = new[] { (0, -1), (0, 1), (-1, 0), (1, 0) };

        for (int seed = 0; seed < 30; seed++)
        {
            var rng = new Random(seed);
            var level = DungeonGenerator.Generate(floorIndex: 5, width: 60, height: 22, rng, difficultyLevel: 5, forceTraderSpawn: false, out var roomRects);

            var visited = new HashSet<(int X, int Y)> { level.StairsUpPosition };
            var queue = new Queue<(int X, int Y)>();
            queue.Enqueue(level.StairsUpPosition);
            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                foreach (var (dx, dy) in orthogonalOffsets)
                {
                    var next = (X: cx + dx, Y: cy + dy);
                    if (visited.Contains(next) || !level.IsWalkable(next.X, next.Y))
                    {
                        continue;
                    }
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }

            bool reachesStairsDown = visited.Contains(level.StairsDownPosition);
            bool reachesEveryRoom = roomRects.All(r => visited.Contains((r.X + r.Width / 2, r.Y + r.Height / 2)));

            if (!reachesStairsDown || !reachesEveryRoom)
            {
                allSeedsFullyConnected = false;
            }
        }

        Check("Every generated floor is fully connected -- every room and the stairs down are reachable from the entrance once doors are open", allSeedsFullyConnected);
    }

    /// <summary>
    /// Flood-fills from the boss's tile across plain floor only, never crossing any door tile
    /// (i.e. treats every door as closed) -- a properly sealed boss room's reachable set
    /// should be a small area that never touches the rest of the floor. If a pre-existing
    /// main-chain corridor leaked through the room's footprint (the exact bug this guards
    /// against -- see IsAreaClear in DungeonGenerator), this flood fill would spill straight
    /// out to the stairs without ever needing to cross a door.
    /// </summary>
    private static bool BossRoomInteriorNeverReachesRestOfFloor(Level level, Monster boss)
    {
        var doorPositions = level.Doors.Select(d => (d.X, d.Y)).ToHashSet();
        var visited = new HashSet<(int X, int Y)> { (boss.X, boss.Y) };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((boss.X, boss.Y));

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            foreach (var (dx, dy) in new[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
            {
                var next = (X: cx + dx, Y: cy + dy);
                if (visited.Contains(next) || doorPositions.Contains(next) || !level.IsWalkable(next.X, next.Y))
                {
                    continue;
                }
                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return !visited.Contains(level.StairsUpPosition) && !visited.Contains(level.StairsDownPosition);
    }

    // --- Trader ------------------------------------------------------------------------

    private static void TraderIsImmuneToTargeting()
    {
        var rng = new Random(30);
        var level = new Level(1, 10, 10);
        var trader = Trader.CreateRandom(1, 1, 5, rng);
        var monster = Monster.CreateRandom(2, 1, 5, rng);
        var player = new Player("ImmunityTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        level.Actors.Add(trader);

        Check("A trader can never be targeted", !trader.CanBeTargeted);
        Check("A monster can be targeted", monster.CanBeTargeted);
        Check("The player can be targeted", player.CanBeTargeted);

        var singleTargetContext = new SpellCastingContext(player, level, turnNumber: 0, rng) { TargetActor = trader };
        var singleTargeting = new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 10);
        bool resolved = TargetResolver.ResolveAffectedActors(singleTargeting, singleTargetContext, out _);
        Check("Single-target spell/skill resolution rejects a trader as a target", !resolved);

        var aoeContext = new SpellCastingContext(player, level, turnNumber: 0, rng) { TargetTile = (trader.X, trader.Y) };
        var aoeTargeting = new SpellTargetingConfiguration(SpellTargetType.Tile, range: 10, areaOfEffectRadius: 5);
        TargetResolver.ResolveAffectedActors(aoeTargeting, aoeContext, out _);
        Check("An AoE spell centered on a trader's tile never includes it in the affected actors", !aoeContext.AffectedActors.Contains(trader));
    }

    private static void TraderNeverActsInTheScheduler()
    {
        var rng = new Random(31);
        var level = new Level(1, 10, 10);
        var trader = Trader.CreateRandom(1, 1, 5, rng);
        level.Actors.Add(trader);
        // Deliberately never registered with level.Scheduler -- see Trader/NPC's own doc comments.

        var player = new Player("SchedulerTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        level.Actors.Add(player);
        level.Scheduler.Register(player);

        bool traderWasEverSelected = false;
        for (int i = 0; i < 50; i++)
        {
            var selected = level.Scheduler.GetNextActor();
            if (selected == null)
            {
                break;
            }
            if (selected == trader)
            {
                traderWasEverSelected = true;
            }
            level.Scheduler.ConsumeEnergy(selected);
        }

        Check("A trader added to a level but never registered with the scheduler is never offered a turn", !traderWasEverSelected);
    }

    private static void TraderSpawnRespectsBossRoomAndStairsExclusion()
    {
        bool anyStairsViolation = false;
        int tradersFound = 0;

        for (int seed = 0; seed < 60; seed++)
        {
            var rng = new Random(seed);
            var level = DungeonGenerator.Generate(floorIndex: 5, width: 60, height: 22, rng, difficultyLevel: 5, forceTraderSpawn: true, out _);
            var trader = level.Actors.OfType<Trader>().FirstOrDefault();
            if (trader == null)
            {
                continue;
            }

            tradersFound++;
            if ((trader.X, trader.Y) == level.StairsUpPosition || (trader.X, trader.Y) == level.StairsDownPosition)
            {
                anyStairsViolation = true;
            }
        }

        // Boss-room exclusion needs no runtime check here -- SpawnTrader only ever picks from
        // `rooms`, which never contains the boss room (see TrySpawnBossRoom's own comment), so
        // a trader landing there is structurally impossible, not just improbable.
        Check("Forcing a trader spawn actually produces one across many seeds", tradersFound > 0);
        Check("A trader is never placed on the stairs", !anyStairsViolation);
    }

    private static void TraderFrequencyGuaranteeNeverMissesThreeFloors()
    {
        var rng = new Random(42);
        var dungeonManager = new DungeonManager(rng);
        var player = new Player("FrequencyTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        dungeonManager.EnterFirstFloor(player);

        int consecutiveWithoutTrader = 0;
        int maxConsecutiveWithoutTrader = 0;
        for (int floor = 1; floor <= 20; floor++)
        {
            dungeonManager.EnterFloor(player, floor);
            bool hasTrader = dungeonManager.CurrentLevel.Actors.OfType<Trader>().Any();
            consecutiveWithoutTrader = hasTrader ? 0 : consecutiveWithoutTrader + 1;
            maxConsecutiveWithoutTrader = Math.Max(maxConsecutiveWithoutTrader, consecutiveWithoutTrader);
        }

        Check("No three consecutive floors are ever generated without a trader",
            maxConsecutiveWithoutTrader <= TraderConfig.MaximumLevelsWithoutTrader);
    }

    private static void TraderInventoryCountAndGoldValueArePopulated()
    {
        var rng = new Random(33);
        bool allCountsInRange = true;
        for (int i = 0; i < 20; i++)
        {
            var trader = Trader.CreateRandom(0, 0, 5, rng);
            // The generator draws TraderMinInventory..TraderMaxInventory times, but a
            // stackable duplicate (see ItemStacking) now merges into an existing entry's
            // Charges instead of adding a new one -- so the entry count can legitimately fall
            // *below* the draw count (every draw after the first of a kind shrinks it further)
            // but can never exceed it, since merging never creates new entries.
            int count = trader.Inventory.Items.Count;
            if (count < 1 || count > TraderConfig.TraderMaxInventory)
            {
                allCountsInRange = false;
            }
        }
        Check("A trader's generated inventory entry count is always non-empty and never exceeds TraderMaxInventory, even after stackable duplicates merge into fewer entries",
            allCountsInRange);

        Check("Every catalog item's GoldValue is auto-computed to a positive value", Items.All.All(i => i.GoldValue > 0));
    }

    private static void CharismaAdjustsTradePrices()
    {
        var rng = new Random(34);

        Player MakePlayerWithCharisma(int adjustedCharisma)
        {
            var p = new Player("ChaTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
            var block = p.Stats.Get(PrimaryAttribute.Charisma);
            block.BaseValue = 10;
            block.ClassModifier = 0;
            block.OtherModifier = adjustedCharisma - 10; // Adjusted = 10 (creation) + OtherModifier
            return p;
        }

        var low = MakePlayerWithCharisma(1);
        var average = MakePlayerWithCharisma(10);
        var high = MakePlayerWithCharisma(18);
        var extreme = MakePlayerWithCharisma(1000); // unreachable via normal stat rolling -- forces the clamp

        var item = Items.FlamingLongSword.Clone();

        Check("Average Charisma buys at exactly the item's base GoldValue",
            ItemPricingCalculator.CalculateBuyPrice(item, average) == item.GoldValue);
        Check("Higher Charisma buys for less than average, which buys for less than low Charisma",
            ItemPricingCalculator.CalculateBuyPrice(item, high) < ItemPricingCalculator.CalculateBuyPrice(item, average)
            && ItemPricingCalculator.CalculateBuyPrice(item, average) < ItemPricingCalculator.CalculateBuyPrice(item, low));
        Check("Higher Charisma sells for more than average, which sells for more than low Charisma",
            ItemPricingCalculator.CalculateSellPrice(item, high) > ItemPricingCalculator.CalculateSellPrice(item, average)
            && ItemPricingCalculator.CalculateSellPrice(item, average) > ItemPricingCalculator.CalculateSellPrice(item, low));

        int extremeBuy = ItemPricingCalculator.CalculateBuyPrice(item, extreme);
        int extremeSell = ItemPricingCalculator.CalculateSellPrice(item, extreme);
        Check("Buy price never drops below its configured minimum multiplier even at extreme Charisma",
            extremeBuy >= (int)Math.Round(item.GoldValue * TraderConfig.MinBuyPriceMultiplier) - 1);
        Check("Sell price never exceeds its configured maximum multiplier even at extreme Charisma",
            extremeSell <= (int)Math.Round(item.GoldValue * TraderConfig.MaxSellPriceMultiplier) + 1);
    }

    private static void TraderBuySellTransactions()
    {
        var rng = new Random(35);
        var player = new Player("BuySellTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var trader = Trader.CreateRandom(0, 0, 3, rng);

        if (trader.Inventory.Items.Count == 0)
        {
            Skip("Trader buy/sell transactions", "generated trader had an empty inventory this run");
            return;
        }

        var item = trader.Inventory.Items[0];
        int price = ItemPricingCalculator.CalculateBuyPrice(item, player);

        player.RestoreGold(0); // zero out starting gold
        TraderScreen.TryBuy(player, trader, 0);
        Check("Buying without enough gold changes nothing",
            trader.Inventory.Items.Contains(item) && !player.Inventory.Items.Contains(item));

        player.RestoreGold(price);
        long goldBeforeBuy = player.Gold;
        TraderScreen.TryBuy(player, trader, 0);
        Check("A successful buy moves the item from the trader to the player",
            !trader.Inventory.Items.Contains(item) && player.Inventory.Items.Contains(item));
        Check("A successful buy deducts exactly the displayed price", player.Gold == goldBeforeBuy - price);

        int sellIndex = player.Inventory.Items.IndexOf(item);
        int sellPrice = ItemPricingCalculator.CalculateSellPrice(item, player);
        long goldBeforeSell = player.Gold;
        TraderScreen.TrySell(player, trader, sellIndex);
        Check("A successful sell moves the item from the player back to the trader",
            !player.Inventory.Items.Contains(item) && trader.Inventory.Items.Contains(item));
        Check("A successful sell awards exactly the displayed sell price", player.Gold == goldBeforeSell + sellPrice);
    }

    private static void TraderIdentificationService()
    {
        var rng = new Random(36);
        var player = new Player("IdentifyTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var unidentified = Items.VenomfangDagger.Clone();
        player.Inventory.AddItem(unidentified);

        player.RestoreGold(0);
        string poorMessage = TraderScreen.TryIdentify(player, unidentified);
        Check("Identification without enough gold is rejected and leaves the item unidentified",
            !unidentified.IsIdentified && poorMessage.Contains("enough"));

        player.RestoreGold(TraderConfig.TraderIdentificationCost);
        long goldBefore = player.Gold;
        TraderScreen.TryIdentify(player, unidentified);
        Check("A successful identification sets IsIdentified and charges exactly the configured cost",
            unidentified.IsIdentified && player.Gold == goldBefore - TraderConfig.TraderIdentificationCost);

        string detailsAfter = InventoryScreen.FormatItemDetails(player, unidentified);
        Check("Identified item details reveal what was hidden (e.g. its status effect)", detailsAfter.Contains("Status Effects"));

        var stillUnidentified = player.Inventory.Items
            .Concat(player.Equipment.AllEquipped.Select(kvp => kvp.Value))
            .Where(i => !i.IsIdentified)
            .ToList();
        Check("An already-identified item is never offered as a candidate for (re-)identification", !stillUnidentified.Contains(unidentified));
    }

    private static void TraderRestoreDataRoundTrip()
    {
        var rng = new Random(37);
        var original = Trader.CreateRandom(5, 7, 4, rng);
        original.Gold = 12345;
        // Simulate a sale having happened -- inventory no longer matches a fresh CreateRandom roll.
        original.Inventory.AddItem(Items.BlessedLongSword.Clone());

        var restoreData = new Trader.RestoreData
        {
            Name = original.Name,
            Symbol = original.Symbol,
            Color = original.Color,
            X = original.X,
            Y = original.Y,
            ShortDescription = original.ShortDescription,
            LongDescription = original.LongDescription,
            Gold = original.Gold,
            Inventory = original.Inventory.Items.ToList()
        };

        var restored = Trader.Restore(restoreData);

        Check("A restored trader has the exact same position, name, and gold as when saved",
            restored.X == original.X && restored.Y == original.Y && restored.Name == original.Name && restored.Gold == original.Gold);
        Check("A restored trader has the exact same remaining inventory as when saved, not a fresh roll",
            restored.Inventory.Items.SequenceEqual(original.Inventory.Items));
        Check("A restored trader is still fully protected", !restored.CanBeTargeted);
    }

    /// <summary>Usability (a hard level/class/race block) takes visual priority over affordability, which is just "not yet" -- see TraderScreen.ItemLineColor's own doc comment.</summary>
    private static void TraderScreenColorsReflectUsabilityAndAffordability()
    {
        Check("A usable, affordable item renders bright", TraderScreen.ItemLineColor(canUse: true, canAfford: true) == ConsoleColor.White);
        Check("A usable but unaffordable item renders dim", TraderScreen.ItemLineColor(canUse: true, canAfford: false) == ConsoleColor.DarkGray);
        Check("An unusable item renders in its own distinct color regardless of affordability",
            TraderScreen.ItemLineColor(canUse: false, canAfford: true) == ConsoleColor.DarkRed
            && TraderScreen.ItemLineColor(canUse: false, canAfford: false) == ConsoleColor.DarkRed);
    }

    // --- Projectile system -------------------------------------------------------------
    // Every level built here has every tile explicitly set to floor (IsVisible stays false,
    // its default) -- ProjectileEngine.Launch requires walkable tiles to travel at all, and
    // leaving IsVisible false means RenderFrame's visibility check skips the real
    // Renderer.Render call entirely, so these run headless with no console output or Thread.Sleep-driven flakiness beyond the animation delay itself.

    private static Level BuildOpenLevel(int width, int height)
    {
        var level = new Level(1, width, height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                level.Tiles[x, y] = Tile.CreateFloor();
            }
        }
        return level;
    }

    private static void ProjectileStopsAtWall()
    {
        var rng = new Random(60);
        var level = BuildOpenLevel(10, 5);
        level.Tiles[5, 2] = Tile.CreateWall();
        var shooter = Monster.Restore(BaseRestoreData("wall-shooter", attack: 5, defense: 0));
        shooter.X = 1;
        shooter.Y = 2;
        var viewer = new Player("WallTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));

        var definition = new ProjectileDefinition("test bolt", range: 10, penetration: 0, displayCharacter: '*', color: ConsoleColor.Gray, attackType: AttackType.Pierce);
        var projectile = new ProjectileInstance(definition, shooter, ProjectileSourceType.Weapon, shooter.X, shooter.Y, (1, 0)) { Damage = 5, DamageType = DamageType.Physical };

        var outcome = ProjectileEngine.Launch(level, viewer, Array.Empty<MessageEntry>(), projectile, rng);

        Check("A projectile stops on the tile before a wall instead of passing through it",
            outcome.Reason == ProjectileTerminationReason.HitWall && projectile.X == 4 && projectile.Y == 2);
    }

    /// <summary>
    /// Same class of bug as BumpAttackingAnInvisibleOccupantNeverRevealsItsName, for the fired-
    /// projectile path -- ProjectileEngine.ResolveCollision never computed/passed
    /// CombatMessages.Format's otherPartyVisible parameter at all, so a shot landing on (or fired
    /// from) an invisible dark-room occupant always named it regardless of visibility.
    /// </summary>
    private static void FiredProjectileHittingAnInvisibleOccupantNeverRevealsItsName()
    {
        var rng = new Random(902);
        var level = BuildOpenLevel(10, 5); // tiles default IsVisible = false
        var shooter = new Player("InvisibleTargetShooterTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng)) { X = 1, Y = 2 };
        var target = Monster.Restore(BaseRestoreData("invisible-target", attack: 5, defense: 0));
        target.X = 3;
        target.Y = 2;
        level.Actors.Add(target);

        var definition = new ProjectileDefinition("test arrow", range: 10, penetration: 0, displayCharacter: '*', color: ConsoleColor.Gray, attackType: AttackType.Pierce);
        var projectile = new ProjectileInstance(definition, shooter, ProjectileSourceType.Weapon, shooter.X, shooter.Y, (1, 0)) { Damage = 10, DamageType = DamageType.Physical };

        ProjectileEngine.Launch(level, shooter, Array.Empty<MessageEntry>(), projectile, rng);

        Check("A shot landing on an invisible occupant produces at least one message", projectile.Messages.Count > 0);
        Check("None of those messages reveal the invisible occupant's true name", !projectile.Messages.Any(m => m.Contains(target.Name)));
        Check("At least one message uses generic wording instead", projectile.Messages.Any(m => m.Contains("something", StringComparison.OrdinalIgnoreCase)));
    }

    private static void ProjectilePenetrationHitsMultipleDistinctTargetsOnce()
    {
        var rng = new Random(61);
        var level = BuildOpenLevel(10, 5);
        // The shooter must be the Player (not a Monster) -- ProjectileEngine's friendly-fire
        // rule never lets a monster-sourced shot hit another monster, only the player beyond it.
        var shooter = new Player("PierceShooter", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng)) { X = 1, Y = 2 };
        var t1 = Monster.Restore(BaseRestoreData("pierce-t1", attack: 0, defense: 0));
        t1.X = 3;
        t1.Y = 2;
        var t2 = Monster.Restore(BaseRestoreData("pierce-t2", attack: 0, defense: 0));
        t2.X = 5;
        t2.Y = 2;
        var t3 = Monster.Restore(BaseRestoreData("pierce-t3", attack: 0, defense: 0));
        t3.X = 7;
        t3.Y = 2;
        level.Actors.Add(t1);
        level.Actors.Add(t2);
        level.Actors.Add(t3);

        // Penetration 2 = "2 creatures beyond the first" -- 3 total, then it stops.
        var definition = new ProjectileDefinition("test spear", range: 10, penetration: 2, displayCharacter: '*', color: ConsoleColor.Gray, attackType: AttackType.Pierce);
        var projectile = new ProjectileInstance(definition, shooter, ProjectileSourceType.Weapon, shooter.X, shooter.Y, (1, 0)) { Damage = 5, DamageType = DamageType.Physical };

        var outcome = ProjectileEngine.Launch(level, shooter, Array.Empty<MessageEntry>(), projectile, rng);

        Check("Penetration lets one projectile hit multiple distinct creatures, never the same one twice",
            outcome.Reason == ProjectileTerminationReason.HitCreature
            && projectile.AlreadyHit.Count == 3
            && projectile.AlreadyHit.Contains(t1) && projectile.AlreadyHit.Contains(t2) && projectile.AlreadyHit.Contains(t3)
            && t1.Health.Current < t1.Health.Max && t2.Health.Current < t2.Health.Max && t3.Health.Current < t3.Health.Max);
    }

    private static void ProjectileFriendlyFireRulesPlayerSource()
    {
        var rng = new Random(62);
        var level = BuildOpenLevel(10, 5);
        var player = new Player("FriendlyFireTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng)) { X = 1, Y = 2 };
        var trader = Trader.CreateRandom(3, 2, 1, rng);
        var monster = Monster.Restore(BaseRestoreData("ff-target", attack: 0, defense: 0));
        monster.X = 5;
        monster.Y = 2;
        level.Actors.Add(trader);
        level.Actors.Add(monster);

        var definition = new ProjectileDefinition("test arrow", range: 10, penetration: 0, displayCharacter: null, color: ConsoleColor.Gray, attackType: AttackType.Pierce);
        var projectile = new ProjectileInstance(definition, player, ProjectileSourceType.Weapon, player.X, player.Y, (1, 0)) { Damage = 5, DamageType = DamageType.Physical };

        var outcome = ProjectileEngine.Launch(level, player, Array.Empty<MessageEntry>(), projectile, rng);

        Check("A player-fired projectile passes harmlessly over a trader and hits the monster beyond it",
            outcome.Reason == ProjectileTerminationReason.HitCreature && projectile.AlreadyHit.Contains(monster)
            && !projectile.AlreadyHit.Contains(trader) && monster.Health.Current < monster.Health.Max);
    }

    private static void ProjectileFriendlyFireRulesMonsterSource()
    {
        var rng = new Random(63);
        var level = BuildOpenLevel(10, 5);
        var shooter = Monster.Restore(BaseRestoreData("ff-shooter", attack: 5, defense: 0));
        shooter.X = 1;
        shooter.Y = 2;
        var bystander = Monster.Restore(BaseRestoreData("ff-bystander", attack: 0, defense: 0));
        bystander.X = 3;
        bystander.Y = 2;
        var player = new Player("FriendlyFireTargetTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng)) { X = 5, Y = 2 };
        level.Actors.Add(bystander);
        level.Actors.Add(player);

        var definition = new ProjectileDefinition("test bolt", range: 10, penetration: 0, displayCharacter: null, color: ConsoleColor.Gray, attackType: AttackType.Pierce);
        var projectile = new ProjectileInstance(definition, shooter, ProjectileSourceType.Weapon, shooter.X, shooter.Y, (1, 0)) { Damage = 5, DamageType = DamageType.Physical };

        int before = player.Health.Current;
        var outcome = ProjectileEngine.Launch(level, player, Array.Empty<MessageEntry>(), projectile, rng);

        Check("A monster-fired projectile never hits another monster, only the player beyond it",
            outcome.Reason == ProjectileTerminationReason.HitCreature && projectile.AlreadyHit.Contains(player)
            && !projectile.AlreadyHit.Contains(bystander) && player.Health.Current < before);
    }

    private static void ProjectileReusesSameStatusEffectPipelineAsMelee()
    {
        var rng = new Random(64);
        var effects = new[] { new ItemStatusEffect(ItemEffectType.Fire, chance: 1.0, magnitude: 6) };
        var weapon = new Item("Test Torch", '/', "test", ItemType.Weapon, EquipmentType.Hand, statusEffects: effects);
        var (attacker, meleeDefender, level) = BuildCombatPair(weapon);
        var projectileDefender = Monster.Restore(BaseRestoreData("proc-defender-proj", attack: 0, defense: 0));
        level.Actors.Add(projectileDefender);

        ItemEffectApplier.ApplyOnHitEffects(ForceHit(attacker, meleeDefender), level, rng);
        ItemEffectApplier.ApplyStatusEffects(attacker, projectileDefender, effects, level, rng);

        Check("A projectile's status effects apply through the exact same shared pipeline melee already uses",
            meleeDefender.Health.Max - meleeDefender.Health.Current == 6
            && projectileDefender.Health.Max - projectileDefender.Health.Current == 6);
    }

    private static double AverageProjectileDamage(Level level, Actor shooter, Monster target, Item weapon, Item ammo, Player viewer, Random rng, int samples)
    {
        if (!level.Actors.Contains(target))
        {
            level.Actors.Add(target);
        }

        var damages = new List<int>();
        for (int i = 0; i < samples; i++)
        {
            target.Health.SetCurrent(target.Health.Max);
            var projectile = ProjectileFactory.ForWeaponAndAmmo(shooter, weapon, ammo, target.X - 1, target.Y, (1, 0));
            ProjectileEngine.Launch(level, viewer, Array.Empty<MessageEntry>(), projectile, rng);
            damages.Add(target.Health.Max - target.Health.Current);
        }

        level.Actors.Remove(target);
        return damages.Average();
    }

    private static void ProjectileAppliesBlessedUndeadBonusJustLikeMelee()
    {
        var rng = new Random(65);
        var level = BuildOpenLevel(10, 5);
        var shooter = new Player("BlessedProjShooter", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng)) { X = 1, Y = 2 };
        var undead = Monster.Restore(BaseRestoreData("blessed-undead-target", attack: 0, defense: 0, CreatureType.Undead));
        undead.X = 2;
        undead.Y = 2;
        var normal = Monster.Restore(BaseRestoreData("blessed-normal-target", attack: 0, defense: 0, CreatureType.Other));
        normal.X = 2;
        normal.Y = 2;

        double avgVsUndead = AverageProjectileDamage(level, shooter, undead, Items.Bow, Items.HolyArrow, shooter, rng, 20);
        double avgVsNormal = AverageProjectileDamage(level, shooter, normal, Items.Bow, Items.HolyArrow, shooter, rng, 20);

        Check("A blessed arrow fired through ProjectileEngine deals its bonus damage only against Undead, same as a blessed melee weapon",
            Math.Abs((avgVsUndead - avgVsNormal) - Items.HolyArrow.BlessedUndeadDamageBonus) < 1.0);
    }

    private static void SpellIsProjectileClassification()
    {
        Check("Fireball, Magic Missile, and Frost Bolt are all classified as projectile spells",
            SpellCatalog.Fireball.IsProjectile && SpellCatalog.MagicMissile.IsProjectile && SpellCatalog.FrostBolt.IsProjectile);
        Check("A pure Self-target spell (Heal) is never classified as a projectile", !SpellCatalog.Heal.IsProjectile);
    }

    private static void SpellCastOnTargetsResolvedHookFiresExactlyOnce()
    {
        var rng = new Random(66);
        var level = BuildOpenLevel(10, 5);
        var caster = Monster.Restore(BaseRestoreData("hook-caster", attack: 0, defense: 0));
        caster.Mana = new ManaComponent(50);
        caster.X = 1;
        caster.Y = 2;
        var target = Monster.Restore(BaseRestoreData("hook-target", attack: 0, defense: 0));
        target.X = 5;
        target.Y = 2;
        level.Actors.Add(caster);
        level.Actors.Add(target);

        int callCount = 0;
        (int X, int Y) capturedPosition = default;
        var context = new SpellCastingContext(caster, level, level.TurnNumber, rng) { TargetActor = target };
        var result = SpellCaster.Cast(SpellCatalog.MagicMissile, context, consumesMana: true, onTargetsResolved: ctx =>
        {
            callCount++;
            capturedPosition = (ctx.TargetActor.X, ctx.TargetActor.Y);
        });

        Check("SpellCaster.Cast's onTargetsResolved hook fires exactly once, after targets are already committed",
            result.Success && callCount == 1 && capturedPosition == (target.X, target.Y));

        var context2 = new SpellCastingContext(caster, level, level.TurnNumber, rng) { TargetActor = target };
        var result2 = SpellCaster.Cast(SpellCatalog.MagicMissile, context2);
        Check("Omitting onTargetsResolved (every pre-existing call site) still casts normally with no hook invoked", result2.Success);
    }

    private static void FiringDoesNotAutoIdentifyTheAmmo()
    {
        var rng = new Random(67);
        var level = BuildOpenLevel(10, 5);
        var shooter = new Player("UnidentifiedAmmoTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng)) { X = 1, Y = 2 };
        var target = Monster.Restore(BaseRestoreData("unident-target", attack: 0, defense: 0));
        target.X = 5;
        target.Y = 2;
        level.Actors.Add(target);

        var ammo = Items.HolyArrow.Clone();
        Check("Fixture sanity check: Holy Arrow starts unidentified", !ammo.IsIdentified);

        var projectile = ProjectileFactory.ForWeaponAndAmmo(shooter, Items.Bow, ammo, shooter.X, shooter.Y, (1, 0));
        ProjectileEngine.Launch(level, shooter, Array.Empty<MessageEntry>(), projectile, rng);

        Check("Firing an unidentified ammo item never auto-identifies it -- mechanics apply while it's still hidden",
            !ammo.IsIdentified && !Items.HolyArrow.IsIdentified);
    }

    private static void ThrownItemsDefaultToDropsAtImpactPointUnlessOverridden()
    {
        Check("A CanBeThrown item with no explicit behavior defaults to DropsAtImpactPoint",
            Items.ThrowingKnife.CanBeThrown && Items.ThrowingKnife.ThrownWeaponBehavior == ThrownWeaponBehavior.DropsAtImpactPoint);

        var destroyedOnThrow = new Item("Test Grenade", '*', "test", ItemType.Weapon,
            canBeThrown: true, thrownWeaponBehavior: ThrownWeaponBehavior.Destroyed);
        Check("An explicit ThrownWeaponBehavior override is respected instead of the default",
            destroyedOnThrow.ThrownWeaponBehavior == ThrownWeaponBehavior.Destroyed);

        Check("A non-throwable item has no ThrownWeaponBehavior at all",
            !Items.Dagger.CanBeThrown && Items.Dagger.ThrownWeaponBehavior == null);
    }

    private static void DeathCausePhraseReadsAsAGrammaticalSentence()
    {
        var rng = new Random(70);
        var weapon = new Item("Test Venom Blade", '/', "test", ItemType.Weapon, EquipmentType.Hand,
            statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Poison, chance: 1.0, magnitude: 3, duration: 3) });
        var (attacker, defender, level) = BuildCombatPair(weapon);

        ItemEffectApplier.ApplyOnHitEffects(ForceHit(attacker, defender), level, rng);

        var poisonEffect = defender.ActiveEffects.FirstOrDefault(e => e.TickDamageType == DamageType.Poison);
        string expectedSuffix = $"poisoned {CombatMessages.AttackWord(attacker.AttackType)}";
        Check("A poison DoT's death-cause description reads as \"X's poisoned <attack verb>\" (e.g. \"poisoned hit\"), not the bare adjective \"X's Poisoned\"",
            poisonEffect != null
            && poisonEffect.DamageSourceDescription.EndsWith(expectedSuffix)
            && !poisonEffect.DamageSourceDescription.Contains("Poisoned"));
    }

    private static void ThrowableCategoryClassGating()
    {
        // Note: AllowedThrowableCategories itself is only ever consulted for Martial/Exotic --
        // ThrowableCategory.Light is special-cased as universally allowed regardless of this
        // list (see DecideFireProjectileAction and ThrowableCategory.Light's own doc comment,
        // "no training needed"). These checks describe the raw per-class list only; see
        // EveryClassCanThrowLightItemsRegardlessOfItsAllowedThrowableCategoriesList for the
        // actual throw-permission behavior a Warrior/Priest gets.
        Check("Warrior's list explicitly grants Martial (knife/axe/spear) but not Exotic",
            CharacterClass.Warrior.AllowedThrowableCategories.Contains(ThrowableCategory.Martial)
            && !CharacterClass.Warrior.AllowedThrowableCategories.Contains(ThrowableCategory.Exotic));

        Check("Thief's list explicitly grants Martial, Exotic, and Light",
            CharacterClass.Thief.AllowedThrowableCategories.Contains(ThrowableCategory.Martial)
            && CharacterClass.Thief.AllowedThrowableCategories.Contains(ThrowableCategory.Exotic)
            && CharacterClass.Thief.AllowedThrowableCategories.Contains(ThrowableCategory.Light));

        Check("Mage's list explicitly grants only Light, never Martial or Exotic",
            CharacterClass.Mage.AllowedThrowableCategories.Contains(ThrowableCategory.Light)
            && !CharacterClass.Mage.AllowedThrowableCategories.Contains(ThrowableCategory.Martial)
            && !CharacterClass.Mage.AllowedThrowableCategories.Contains(ThrowableCategory.Exotic));

        Check("Catalog: Throwing Knife/Axe/Short Spear/Long Spear are Martial, Shuriken is Exotic, Dart/Rock are Light",
            Items.ThrowingKnife.ThrowableCategory == ThrowableCategory.Martial
            && Items.ThrowingAxe.ThrowableCategory == ThrowableCategory.Martial
            && Items.ShortSpear.ThrowableCategory == ThrowableCategory.Martial
            && Items.LongSpear.ThrowableCategory == ThrowableCategory.Martial
            && Items.Shuriken.ThrowableCategory == ThrowableCategory.Exotic
            && Items.Dart.ThrowableCategory == ThrowableCategory.Light
            && Items.Rock.ThrowableCategory == ThrowableCategory.Light);
    }

    /// <summary>
    /// Regression coverage for a reported bug: the Trader buy/sell screen colored a Spellbook
    /// of Smite and other class-inappropriate items bright white (canUse: true) for a Warrior,
    /// because ItemRequirementValidator.CanUse only ever checked Level/RequiredClass/
    /// RequiredRace -- never whether the class could actually equip the item (WeaponType/
    /// ArmorWeight/shield) or learn what it teaches (TeachesSpell). Fixed by folding in
    /// EquipmentCompatibility.IsAllowedForClass and Spell.CanBeCastBy, with an OR (not AND)
    /// between equip- and throw-eligibility for a dual-purpose item so a Thief throwing an
    /// Axe they could never wield in melee doesn't regress.
    /// </summary>
    private static void ShieldSizeRestrictionsMatchDesign()
    {
        Check("Warrior and Priest can use any shield size",
            CharacterClass.Warrior.AllowedShieldSizes.Contains(Size.Small) && CharacterClass.Warrior.AllowedShieldSizes.Contains(Size.Medium) && CharacterClass.Warrior.AllowedShieldSizes.Contains(Size.Large)
            && CharacterClass.Priest.AllowedShieldSizes.Contains(Size.Small) && CharacterClass.Priest.AllowedShieldSizes.Contains(Size.Medium) && CharacterClass.Priest.AllowedShieldSizes.Contains(Size.Large));

        Check("Thief can use only small shields",
            CharacterClass.Thief.AllowedShieldSizes.Contains(Size.Small)
            && !CharacterClass.Thief.AllowedShieldSizes.Contains(Size.Medium)
            && !CharacterClass.Thief.AllowedShieldSizes.Contains(Size.Large));

        Check("Mage cannot use any shield size", CharacterClass.Mage.AllowedShieldSizes.Count == 0);

        Check("A Thief can equip a small shield (Round Shield) but not a medium one (Wooden Shield)",
            EquipmentCompatibility.IsAllowedForClass(CharacterClass.Thief, Items.RoundShield, out _)
            && !EquipmentCompatibility.IsAllowedForClass(CharacterClass.Thief, Items.Shield, out _));

        Check("A Mage cannot equip even the smallest shield",
            !EquipmentCompatibility.IsAllowedForClass(CharacterClass.Mage, Items.RoundShield, out _));

        Check("A Warrior can equip a large shield (Tower Shield)",
            EquipmentCompatibility.IsAllowedForClass(CharacterClass.Warrior, Items.TowerShield, out _));
    }

    /// <summary>Thief is limited to small/medium weapons (Dagger, Short Sword, Rapier, Bow) -- no Large ones like the Long Sword or Broadsword -- same "light and fast" shape as the Small-only shield restriction above.</summary>
    private static void ThiefWeaponSizeRestrictionsMatchDesign()
    {
        Check("Warrior, Priest, and Mage can use any weapon size",
            CharacterClass.Warrior.AllowedWeaponSizes.Contains(Size.Small) && CharacterClass.Warrior.AllowedWeaponSizes.Contains(Size.Medium) && CharacterClass.Warrior.AllowedWeaponSizes.Contains(Size.Large)
            && CharacterClass.Priest.AllowedWeaponSizes.Contains(Size.Small) && CharacterClass.Priest.AllowedWeaponSizes.Contains(Size.Medium) && CharacterClass.Priest.AllowedWeaponSizes.Contains(Size.Large)
            && CharacterClass.Mage.AllowedWeaponSizes.Contains(Size.Small) && CharacterClass.Mage.AllowedWeaponSizes.Contains(Size.Medium) && CharacterClass.Mage.AllowedWeaponSizes.Contains(Size.Large));

        Check("Thief can use only small and medium weapons",
            CharacterClass.Thief.AllowedWeaponSizes.Contains(Size.Small)
            && CharacterClass.Thief.AllowedWeaponSizes.Contains(Size.Medium)
            && !CharacterClass.Thief.AllowedWeaponSizes.Contains(Size.Large));

        Check("A Thief can equip a Dagger (small) and a Short Sword (medium)",
            EquipmentCompatibility.IsAllowedForClass(CharacterClass.Thief, Items.Dagger, out _)
            && EquipmentCompatibility.IsAllowedForClass(CharacterClass.Thief, Items.ShortSword, out _));

        Check("A Thief cannot equip a Long Sword (large) or a Broadsword (large)",
            !EquipmentCompatibility.IsAllowedForClass(CharacterClass.Thief, Items.LongSword, out string longSwordReason)
            && longSwordReason == "Thiefs cannot use a large weapon."
            && !EquipmentCompatibility.IsAllowedForClass(CharacterClass.Thief, Items.Broadsword, out _));

        Check("A Warrior can still equip a Long Sword (large weapons stay unrestricted for other classes)",
            EquipmentCompatibility.IsAllowedForClass(CharacterClass.Warrior, Items.LongSword, out _));

        Check("A Thief cannot actually equip a Long Sword through the full CanEquip gate",
            !EquipmentCompatibility.CanEquip(
                new Player("ThiefWeaponSizeTester", CharacterClass.Thief, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Thief, new Random(230))),
                Items.LongSword, EquipmentSlot.PrimaryHand, out _));
    }

    // --- Floor tile type system --------------------------------------------------------

    private static void FloorTypeDefaultsToNormal()
    {
        Check("A brand-new Tile defaults to FloorType.Normal", new Tile().FloorType == FloorType.Normal);
        Check("Tile.CreateFloor() also defaults to FloorType.Normal (backward-compatible with every existing spawn site)",
            Tile.CreateFloor().FloorType == FloorType.Normal);
    }

    private static void FloorTypeVisualDefinitionsMatchSpec()
    {
        Check("Water is '~' + Blue", FloorTypeCatalog.Water.DisplayCharacter == '~' && FloorTypeCatalog.Water.DisplayColor == ConsoleColor.Blue);
        Check("Ice is '=' + Cyan", FloorTypeCatalog.Ice.DisplayCharacter == '=' && FloorTypeCatalog.Ice.DisplayColor == ConsoleColor.Cyan);
        Check("Fire is '^' + Red", FloorTypeCatalog.Fire.DisplayCharacter == '^' && FloorTypeCatalog.Fire.DisplayColor == ConsoleColor.Red);
        Check("Lava is '~' + DarkRed -- same glyph as Water, told apart only by color and FloorType itself",
            FloorTypeCatalog.Lava.DisplayCharacter == '~' && FloorTypeCatalog.Lava.DisplayColor == ConsoleColor.DarkRed);
        Check("Fire and Lava both flag EnvironmentalDamageEnabled with DamageType.Fire; every other floor type does not",
            FloorTypeCatalog.Fire.EnvironmentalDamageEnabled && FloorTypeCatalog.Fire.EnvironmentalDamageType == DamageType.Fire
            && FloorTypeCatalog.Lava.EnvironmentalDamageEnabled && FloorTypeCatalog.Lava.EnvironmentalDamageType == DamageType.Fire
            && !FloorTypeCatalog.Normal.EnvironmentalDamageEnabled && !FloorTypeCatalog.Water.EnvironmentalDamageEnabled && !FloorTypeCatalog.Ice.EnvironmentalDamageEnabled);
    }

    private static void FloorTypeElementalMultipliersMatchSpec()
    {
        Check("Water: Shock x1.5, Fire x0.5, Ice/Water/Physical unchanged",
            FloorTypeCatalog.Water.MultiplierFor(DamageType.Lightning) == 1.5
            && FloorTypeCatalog.Water.MultiplierFor(DamageType.Fire) == 0.5
            && FloorTypeCatalog.Water.MultiplierFor(DamageType.Ice) == 1.0
            && FloorTypeCatalog.Water.MultiplierFor(DamageType.Water) == 1.0
            && FloorTypeCatalog.Water.MultiplierFor(DamageType.Physical) == 1.0);

        Check("Fire: Fire x1.5, Water x0.5, Ice x0.5, Shock unchanged",
            FloorTypeCatalog.Fire.MultiplierFor(DamageType.Fire) == 1.5
            && FloorTypeCatalog.Fire.MultiplierFor(DamageType.Water) == 0.5
            && FloorTypeCatalog.Fire.MultiplierFor(DamageType.Ice) == 0.5
            && FloorTypeCatalog.Fire.MultiplierFor(DamageType.Lightning) == 1.0);

        Check("Lava has the exact same elemental multipliers as Fire",
            FloorTypeCatalog.Lava.MultiplierFor(DamageType.Fire) == 1.5
            && FloorTypeCatalog.Lava.MultiplierFor(DamageType.Water) == 0.5
            && FloorTypeCatalog.Lava.MultiplierFor(DamageType.Ice) == 0.5);

        Check("Non-elemental damage (Poison, Physical, Arcane) is always 1.0x on any floor",
            FloorTypeCatalog.Fire.MultiplierFor(DamageType.Poison) == 1.0
            && FloorTypeCatalog.Water.MultiplierFor(DamageType.Physical) == 1.0
            && FloorTypeCatalog.Lava.MultiplierFor(DamageType.Arcane) == 1.0);
    }

    private static void FloorDamageCalculatorAppliesTheRightMultiplier()
    {
        var rng = new Random(120);
        var level = BuildOpenLevel(5, 5);
        var target = Monster.CreateRandom(2, 2, 1, rng);
        level.Actors.Add(target);

        level.Tiles[2, 2].FloorType = FloorType.Fire;
        Check("A Fire-type hit against a target standing on Fire deals 1.5x (10 -> 15)",
            FloorDamageCalculator.ApplyFloorMultiplier(level, target, DamageType.Fire, 10) == 15);
        Check("A Water-type hit against the same target on Fire is unaffected (Fire floor has no Water-specific rule of its own beyond dampening Fire/Ice)",
            FloorDamageCalculator.ApplyFloorMultiplier(level, target, DamageType.Physical, 10) == 10);

        level.Tiles[2, 2].FloorType = FloorType.Water;
        Check("A Fire-type hit against a target standing on Water deals 0.5x (10 -> 5)",
            FloorDamageCalculator.ApplyFloorMultiplier(level, target, DamageType.Fire, 10) == 5);
        Check("A dampening multiplier never rounds a hit down to 0 -- floors at 1",
            FloorDamageCalculator.ApplyFloorMultiplier(level, target, DamageType.Fire, 1) == 1);
        Check("Zero or negative incoming damage is passed through unchanged, never inflated by a floor multiplier",
            FloorDamageCalculator.ApplyFloorMultiplier(level, target, DamageType.Fire, 0) == 0);
    }

    private static void EnvironmentalDamageOnlyFromFireAndLava()
    {
        var player = new Player("EnvDamageTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(121))) { X = 1, Y = 1 };
        var level = new Level(1, 5, 5);
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                level.Tiles[x, y] = Tile.CreateFloor();
            }
        }

        foreach (var floorType in new[] { FloorType.Normal, FloorType.Water, FloorType.Ice, FloorType.Mud, FloorType.Grass })
        {
            level.Tiles[1, 1].FloorType = floorType;
            int before = player.Health.Current;
            string message = EnvironmentalFloorEffects.Apply(level, player);
            Check($"{floorType} causes no environmental damage",
                message == null && player.Health.Current == before);
        }

        level.Tiles[1, 1].FloorType = FloorType.Fire;
        int beforeFire = player.Health.Current;
        string fireMessage = EnvironmentalFloorEffects.Apply(level, player);
        Check("Fire causes environmental damage exactly once per call, with a message",
            fireMessage != null && player.Health.Current < beforeFire);
        Check("The Fire environmental damage message uses a severity word, never the raw damage number",
            fireMessage != null && !fireMessage.Any(char.IsDigit));

        level.Tiles[1, 1].FloorType = FloorType.Lava;
        player.Health.Heal(player.Health.Max);
        int beforeLava = player.Health.Current;
        string lavaMessage = EnvironmentalFloorEffects.Apply(level, player);
        Check("Lava causes environmental damage exactly once per call, with a message",
            lavaMessage != null && player.Health.Current < beforeLava);
        Check("The Lava environmental damage message uses a severity word, never the raw damage number",
            lavaMessage != null && !lavaMessage.Any(char.IsDigit));
    }

    private static void EnvironmentalDamageScalesWithDungeonLevelAndLavaExceedsFire()
    {
        // Thief + Human + CON pinned to 10 (0 modifier) nets exactly 0 Fire resistance from
        // every source (Race.Human=+0, CharacterClass.Thief=+0, CON-10=+0, no equipment/buffs)
        // -- needed so this test's raw-formula assertions aren't skewed by the Resistance
        // System now reducing environmental Fire/Lava damage (design spec section 60).
        Player MakeZeroFireResistancePlayer(string name)
        {
            var stats = new CharacterStats();
            stats.Get(PrimaryAttribute.Constitution).BaseValue = 10;
            var player = new Player(name, CharacterClass.Thief, Race.Human, stats) { X = 1, Y = 1 };
            return player;
        }

        int FireDamageAt(int dungeonLevel)
        {
            var level = new Level(dungeonLevel, 3, 3);
            for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) level.Tiles[x, y] = Tile.CreateFloor();
            level.Tiles[1, 1].FloorType = FloorType.Fire;
            var player = MakeZeroFireResistancePlayer("FireScaleTester");
            int before = player.Health.Current;
            EnvironmentalFloorEffects.Apply(level, player);
            return before - player.Health.Current;
        }

        int LavaDamageAt(int dungeonLevel)
        {
            var level = new Level(dungeonLevel, 3, 3);
            for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) level.Tiles[x, y] = Tile.CreateFloor();
            level.Tiles[1, 1].FloorType = FloorType.Lava;
            var player = MakeZeroFireResistancePlayer("LavaScaleTester");
            int before = player.Health.Current;
            EnvironmentalFloorEffects.Apply(level, player);
            return before - player.Health.Current;
        }

        Check("Fire damage at dungeon level 5 is greater than at level 1", FireDamageAt(5) > FireDamageAt(1));
        Check("Lava damage at dungeon level 5 is greater than at level 1", LavaDamageAt(5) > LavaDamageAt(1));
        Check("Lava deals more environmental damage than Fire at the same dungeon level",
            LavaDamageAt(1) > FireDamageAt(1) && LavaDamageAt(5) > FireDamageAt(5));
        Check("Fire/Lava damage formulas match the spec's own worked examples exactly (Fire lvl1=2, lvl5=10; Lava lvl1=4, lvl5=16)",
            FireDamageAt(1) == 2 && FireDamageAt(5) == 10 && LavaDamageAt(1) == 4 && LavaDamageAt(5) == 16);
    }

    /// <summary>Generates many dungeon floors and validates the room floor-generation rules across all of them, rather than relying on one lucky/unlucky seed -- special floor rooms are random enough (0-3 rooms, random type, random coverage) that a single seed could easily miss exercising the rule being tested.</summary>
    private static void DungeonRoomsNeverMixTwoSpecialFloorTypes()
    {
        int specialRoomsSeen = 0;
        bool anyRoomMixedTypes = false;

        for (int seed = 0; seed < 60; seed++)
        {
            var rng = new Random(seed);
            var level = DungeonGenerator.Generate(1, 60, 30, rng, difficultyLevel: 1, forceTraderSpawn: false, out _, out var roomFloorInfo);

            foreach (var room in roomFloorInfo)
            {
                if (!room.IsSpecialFloorRoom)
                {
                    continue;
                }
                specialRoomsSeen++;

                var uniqueNonNormalTypes = new HashSet<FloorType>();
                for (int x = room.X; x < room.X + room.Width; x++)
                {
                    for (int y = room.Y; y < room.Y + room.Height; y++)
                    {
                        var floorType = level.Tiles[x, y].FloorType;
                        if (floorType != FloorType.Normal)
                        {
                            uniqueNonNormalTypes.Add(floorType);
                        }
                    }
                }

                if (uniqueNonNormalTypes.Count > 1 || (uniqueNonNormalTypes.Count == 1 && !uniqueNonNormalTypes.Contains(room.SpecialFloorType)))
                {
                    anyRoomMixedTypes = true;
                }
            }
        }

        Check("Special floor rooms were actually generated across the sampled seeds (test is exercising something)", specialRoomsSeen > 0);
        Check("No room ever contains more than one non-Normal FloorType, and it always matches the room's recorded SpecialFloorType",
            !anyRoomMixedTypes);
    }

    private static void DungeonSpecialFloorRoomsMeetMinimumCoverage()
    {
        int specialRoomsSeen = 0;
        bool anyBelowMinimum = false;

        for (int seed = 0; seed < 60; seed++)
        {
            var rng = new Random(seed);
            var level = DungeonGenerator.Generate(1, 60, 30, rng, difficultyLevel: 1, forceTraderSpawn: false, out _, out var roomFloorInfo);

            foreach (var room in roomFloorInfo)
            {
                if (!room.IsSpecialFloorRoom)
                {
                    continue;
                }
                specialRoomsSeen++;

                int totalWalkable = 0, specialCount = 0;
                for (int x = room.X; x < room.X + room.Width; x++)
                {
                    for (int y = room.Y; y < room.Y + room.Height; y++)
                    {
                        if (level.Tiles[x, y].Type == TileType.Floor)
                        {
                            totalWalkable++;
                            if (level.Tiles[x, y].FloorType == room.SpecialFloorType)
                            {
                                specialCount++;
                            }
                        }
                    }
                }

                int minimumRequired = (int)Math.Ceiling(totalWalkable * FloorTypeConfig.MinimumSpecialFloorCoverage);
                if (specialCount < minimumRequired)
                {
                    anyBelowMinimum = true;
                }
                // Room.SpecialFloorCoverage should also reflect the same figure actually painted.
                if (Math.Abs(room.SpecialFloorCoverage - (specialCount / (double)totalWalkable)) > 0.001)
                {
                    anyBelowMinimum = true;
                }
            }
        }

        Check("Every special floor room covers at least Ceiling(walkable tiles * 25%), and its recorded SpecialFloorCoverage matches the actual painted tiles",
            specialRoomsSeen > 0 && !anyBelowMinimum);
    }

    private static void DungeonHallwaysAndOtherRoomsStayNormal()
    {
        bool anyHallwayTileNonNormal = false;
        int hallwayFloorTilesChecked = 0;

        for (int seed = 0; seed < 30; seed++)
        {
            var rng = new Random(seed);
            var level = DungeonGenerator.Generate(1, 60, 30, rng, difficultyLevel: 1, forceTraderSpawn: false, out var roomRects, out _);

            for (int x = 0; x < level.Width; x++)
            {
                for (int y = 0; y < level.Height; y++)
                {
                    if (level.Tiles[x, y].Type != TileType.Floor)
                    {
                        continue;
                    }

                    bool insideAnyRoom = roomRects.Any(r => x >= r.X && x < r.X + r.Width && y >= r.Y && y < r.Y + r.Height);
                    if (insideAnyRoom)
                    {
                        continue;
                    }

                    // A plain Floor tile outside every known room rectangle is a hallway tile.
                    hallwayFloorTilesChecked++;
                    if (level.Tiles[x, y].FloorType != FloorType.Normal)
                    {
                        anyHallwayTileNonNormal = true;
                    }
                }
            }
        }

        Check("Hallway tiles were actually sampled across the generated floors (test is exercising something)", hallwayFloorTilesChecked > 0);
        Check("Every hallway/corridor tile stays FloorType.Normal, even on floors with special-floor rooms",
            !anyHallwayTileNonNormal);
    }

    private static void TraderNeverSpawnsAdjacentToADoor()
    {
        bool anyTraderAdjacentToDoor = false;
        int tradersChecked = 0;

        for (int seed = 0; seed < 200; seed++)
        {
            var rng = new Random(seed);
            var level = DungeonGenerator.Generate(1, 60, 30, rng, difficultyLevel: 1, forceTraderSpawn: true);

            foreach (var trader in level.Actors.OfType<Trader>())
            {
                tradersChecked++;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if ((dx != 0 || dy != 0) && level.GetDoorAt(trader.X + dx, trader.Y + dy) != null)
                        {
                            anyTraderAdjacentToDoor = true;
                        }
                    }
                }
            }
        }

        Check("Traders were actually spawned across the sampled seeds (test is exercising something)", tradersChecked > 0);
        Check("No trader ever spawns adjacent (including diagonally) to a door -- it would permanently block the only way through",
            !anyTraderAdjacentToDoor);
    }

    private static void SkeletonKeysStackByCharges()
    {
        var rng = new Random(130);
        var player = new Player("KeyStackTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        // See DeathAndPickupMessagesNeverLeakAnUnidentifiedItemsName's identical comment -- a
        // randomly generated item can occasionally land on the player's own spawn tile.
        level.GroundItems.RemoveAll(drop => drop.X == player.X && drop.Y == player.Y);

        Check("Items.SkeletonKey defaults to exactly 1 charge", Items.SkeletonKey.Charges == 1);

        level.AddItem(player.X, player.Y, Items.SkeletonKey.Clone());
        gameLoop.HandlePickUp(level);
        Check("Picking up the first Skeleton Key adds it to inventory as its own entry with 1 charge",
            player.Inventory.Items.Count(i => i.IsSkeletonKey) == 1 && player.Inventory.Items.First(i => i.IsSkeletonKey).Charges == 1);

        level.AddItem(player.X, player.Y, Items.SkeletonKey.Clone());
        gameLoop.HandlePickUp(level);
        Check("Picking up a second Skeleton Key stacks onto the first instead of adding a new inventory entry",
            player.Inventory.Items.Count(i => i.IsSkeletonKey) == 1 && player.Inventory.Items.First(i => i.IsSkeletonKey).Charges == 2);
    }

    private static void SkeletonKeyDecrementsChargesInsteadOfAlwaysRemoving()
    {
        var rng = new Random(131);
        var player = new Player("KeyUseTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);

        var keyStack = Items.SkeletonKey.Clone();
        keyStack.Charges = 2;
        player.Inventory.AddItem(keyStack);

        // Neither pickable nor bashable -- "Use a skeleton key" is the only offered method, so
        // HandleDoorBump resolves it without ever needing an interactive MenuPrompt choice.
        var door = new Door(0, 0, isLocked: true, isPickable: false, isBashable: false, difficulty: 99, maxHealth: 100);

        gameLoop.HandleDoorBump(door);
        Check("Using a 2-charge Skeleton Key stack once unlocks the door and leaves 1 charge, without removing the item",
            !door.IsLocked && door.IsOpen && player.Inventory.Items.Any(i => i.IsSkeletonKey && i.Charges == 1));

        door.IsLocked = true;
        door.IsOpen = false;
        gameLoop.HandleDoorBump(door);
        Check("Using the last charge unlocks the door and removes the key from inventory entirely",
            !door.IsLocked && door.IsOpen && !player.Inventory.Items.Any(i => i.IsSkeletonKey));
    }

    /// <summary>Regression guard for a bug this same change could easily have reintroduced: SpawnSkeletonKeys used to hand out the shared Items.SkeletonKey catalog reference directly, which was harmless before Charges existed but would have meant every floor's "independent" key secretly shared one mutable Charges value once it did.</summary>
    private static void SkeletonKeySpawnsAreIndependentInstancesNotASharedReference()
    {
        Item firstFloorKey = null, secondFloorKey = null;

        for (int seed = 0; seed < 100 && (firstFloorKey == null || secondFloorKey == null); seed++)
        {
            var level = DungeonGenerator.Generate(1, 60, 30, new Random(seed), difficultyLevel: 1, forceTraderSpawn: false);
            var key = level.GroundItems.Select(g => g.Item).FirstOrDefault(i => i.IsSkeletonKey);
            if (key == null)
            {
                continue;
            }
            if (firstFloorKey == null)
            {
                firstFloorKey = key;
            }
            else
            {
                secondFloorKey = key;
            }
        }

        Check("Two separately generated floors' Skeleton Keys are independent instances, never the shared Items.SkeletonKey reference",
            firstFloorKey != null && secondFloorKey != null
            && !ReferenceEquals(firstFloorKey, Items.SkeletonKey) && !ReferenceEquals(secondFloorKey, Items.SkeletonKey)
            && !ReferenceEquals(firstFloorKey, secondFloorKey));
    }

    /// <summary>Every category the user explicitly asked for -- "all throwable items, all potions, all keys, all scrolls, lockpicks" -- plus Spellbooks (a deliberate consistency extension, see ItemStacking's own doc comment).</summary>
    private static void ItemStackingCoversEveryRequestedCategory()
    {
        Check("A Consumable (potion) is stackable", ItemStacking.IsStackable(Items.HealthPotion));
        Check("Scroll of Identify is stackable", ItemStacking.IsStackable(Items.ScrollOfIdentify));
        Check("An auto-generated spell scroll is stackable", ItemStacking.IsStackable(SpellScrollCatalog.All[0]));
        Check("An auto-generated priest spellbook is stackable", ItemStacking.IsStackable(SpellbookCatalog.All[0]));
        Check("A Skeleton Key (ItemType.Key) is stackable", ItemStacking.IsStackable(Items.SkeletonKey));
        Check("Lockpicks are stackable", ItemStacking.IsStackable(Items.Lockpicks));
        Check("A Throwing Knife (CanBeThrown) is stackable", ItemStacking.IsStackable(Items.ThrowingKnife));
        Check("A Dart (CanBeThrown) is stackable", ItemStacking.IsStackable(Items.Dart));
        Check("A Rock (CanBeThrown) is stackable", ItemStacking.IsStackable(Items.Rock));
        Check("A Shuriken (CanBeThrown) is stackable", ItemStacking.IsStackable(Items.Shuriken));
        Check("Ammunition (Arrow) is stackable -- fired via a bow/crossbow rather than thrown, so it needs its own explicit ItemType.Ammunition case, not just the CanBeThrown branch",
            ItemStacking.IsStackable(Items.Arrow));

        Check("Only Consumables are capped at 5 per stack -- every other stackable category is unlimited",
            ItemStacking.MaxStackSizeFor(Items.HealthPotion) == ItemStacking.MaxPotionStackSize
            && ItemStacking.MaxStackSizeFor(Items.ScrollOfIdentify) == null
            && ItemStacking.MaxStackSizeFor(Items.SkeletonKey) == null
            && ItemStacking.MaxStackSizeFor(Items.Lockpicks) == null
            && ItemStacking.MaxStackSizeFor(Items.ThrowingKnife) == null
            && ItemStacking.MaxStackSizeFor(Items.Arrow) == null);

        var plainSword = new Item("Test Sword", '/', "test", ItemType.Weapon, weaponType: WeaponType.Sword);
        Check("A plain melee weapon (not in any stackable category) is not stackable", !ItemStacking.IsStackable(plainSword));
    }

    private static void PotionStackingCapsAtFiveThenStartsANewStack()
    {
        var rng = new Random(140);
        var player = new Player("PotionStackTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        // See DeathAndPickupMessagesNeverLeakAnUnidentifiedItemsName's identical comment -- a
        // randomly generated item can occasionally land on the player's own spawn tile.
        level.GroundItems.RemoveAll(drop => drop.X == player.X && drop.Y == player.Y);

        for (int i = 0; i < 5; i++)
        {
            level.AddItem(player.X, player.Y, Items.HealthPotion.Clone());
            gameLoop.HandlePickUp(level);
        }
        Check("Picking up 5 Health Potions one at a time merges them into a single stack of 5",
            player.Inventory.Items.Count(i => i.Name == "Health Potion") == 1
            && player.Inventory.Items.First(i => i.Name == "Health Potion").Charges == 5);

        level.AddItem(player.X, player.Y, Items.HealthPotion.Clone());
        gameLoop.HandlePickUp(level);
        Check("A 6th Health Potion starts a new stack instead of exceeding the 5-per-stack cap",
            player.Inventory.Items.Count(i => i.Name == "Health Potion" && i.Charges == 5) == 1
            && player.Inventory.Items.Count(i => i.Name == "Health Potion" && i.Charges == 1) == 1);
    }

    /// <summary>ApplyItem/HandleLearnSpell/ThrowItem all use the same ItemStacking.ConsumeOne helper, so exercising it once via ApplyItem (the only one of the three not gated behind an interactive MenuPrompt.Choose) covers the shared decrement-not-remove logic for all three call sites.</summary>
    private static void ConsumingOneUnitFromAStackDecrementsInsteadOfRemovingTheWholeStack()
    {
        var rng = new Random(141);
        var player = new Player("ConsumeOneTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;

        var potionStack = Items.HealthPotion.Clone();
        potionStack.Charges = 3;
        player.Inventory.AddItem(potionStack);

        InventoryScreen.ApplyItem(player, potionStack, level);
        Check("Drinking one potion from a stack of 3 leaves 2 in the stack instead of removing it entirely",
            player.Inventory.Items.Contains(potionStack) && potionStack.Charges == 2);

        potionStack.Charges = 1;
        InventoryScreen.ApplyItem(player, potionStack, level);
        Check("Drinking the last potion in a stack removes it from inventory entirely",
            !player.Inventory.Items.Contains(potionStack));
    }

    private static void TraderStockMergesStackableItemsInsteadOfListingDuplicates()
    {
        bool anyDuplicateListing = false;
        bool anyMergeObserved = false;
        int tradersChecked = 0;

        for (int seed = 0; seed < 60; seed++)
        {
            var trader = Trader.CreateRandom(0, 0, difficultyLevel: 5, new Random(seed));
            tradersChecked++;

            foreach (var group in trader.Inventory.Items.Where(ItemStacking.IsStackable).GroupBy(i => (i.Name, i.IsIdentified)))
            {
                if (group.Count() > 1)
                {
                    anyDuplicateListing = true;
                }
                else if ((group.First().Charges ?? 1) > 1)
                {
                    anyMergeObserved = true;
                }
            }
        }

        Check("Traders were actually generated across the sampled seeds (test is exercising something)", tradersChecked > 0);
        Check("Trader stock never lists the same stackable item as two separate entries -- a duplicate draw merges into the existing entry's Charges count",
            !anyDuplicateListing);
        Check("At least one sampled trader's stock actually merged multiple draws of the same stackable item into a single entry",
            anyMergeObserved);
    }

    /// <summary>Regression guard: GoldValue used to be frozen at construction time off the constructor's `charges` snapshot, so a stack that grew after spawning (via ItemStacking merging) never got any more expensive to buy/sell than a single unit -- see Item.GoldValue's own doc comment.</summary>
    private static void StackableItemGoldValueScalesWithCurrentChargesNotFrozenAtSpawnTime()
    {
        var potion = Items.HealthPotion.Clone();
        potion.Charges = 1;
        int priceAtOne = potion.GoldValue;

        potion.Charges = 5;
        int priceAtFive = potion.GoldValue;
        Check("A potion stack's GoldValue increases as its Charges count grows after spawning, instead of staying frozen at its spawn-time value",
            priceAtFive > priceAtOne);

        var clonedAgain = potion.Clone();
        Check("Cloning a default-priced item keeps its price dynamic (tracks the clone's own Charges) instead of freezing at the source's GoldValue",
            clonedAgain.Charges == 5 && clonedAgain.GoldValue == priceAtFive);
    }

    private static void TraderStackSuffixShowsQuantityOnlyWhenGreaterThanOne()
    {
        var singlePotion = Items.HealthPotion.Clone();
        singlePotion.Charges = 1;
        Check("A single-charge stackable item shows no quantity suffix", TraderScreen.StackSuffix(singlePotion) == "");

        var stackedPotions = Items.HealthPotion.Clone();
        stackedPotions.Charges = 5;
        Check("A stack of 5 shows an \" x5\" suffix", TraderScreen.StackSuffix(stackedPotions) == " x5");

        var wand = Items.WandOfFire.Clone();
        Check("A non-stackable charge item (a Wand) never gets a stack suffix even though it has Charges",
            TraderScreen.StackSuffix(wand) == "");
    }

    // --- Tile-attuned monster system ------------------------------------------------

    private static void ElementalOppositionTableMatchesDesignSpec()
    {
        Check("Fire's opposing element is Water", ElementalOpposition.GetOpposingElement(DamageType.Fire) == DamageType.Water);
        Check("Water's opposing element is Fire", ElementalOpposition.GetOpposingElement(DamageType.Water) == DamageType.Fire);
        Check("Earth's opposing element is Air", ElementalOpposition.GetOpposingElement(DamageType.Earth) == DamageType.Air);
        Check("Air's opposing element is Earth", ElementalOpposition.GetOpposingElement(DamageType.Air) == DamageType.Earth);
        Check("Ice's opposing element is Fire (one-directional -- Fire's own opposite stays Water)", ElementalOpposition.GetOpposingElement(DamageType.Ice) == DamageType.Fire);
        Check("An element with no defined opposite (e.g. Physical) has none", ElementalOpposition.GetOpposingElement(DamageType.Physical) == null);
        Check("Null in, null out", ElementalOpposition.GetOpposingElement(null) == null);
    }

    /// <summary>Spot-checks one archetype per FloorType via CreateFloorAttuned -- confirms the roster added for the tile-attuned monster spec actually carries the PreferredFloorType/ElementalAffinity/derived-OpposingElement combination the spec's own per-biome tables specify (sections 26-34), rather than just compiling.</summary>
    private static void FloorAttunedArchetypesHaveCorrectPreferredFloorAffinityAndOpposingElement()
    {
        void CheckBiome(FloorType floorType, DamageType? expectedAffinity, DamageType? expectedOpposing)
        {
            var monster = Monster.CreateFloorAttuned(0, 0, difficultyLevel: 1, floorType, new Random(1));
            Check($"A {floorType} room can spawn a {floorType}-attuned monster", monster != null);
            if (monster == null)
            {
                return;
            }
            Check($"{floorType}-attuned monster's PreferredFloorType is {floorType}", monster.PreferredFloorType == floorType);
            Check($"{floorType}-attuned monster's ElementalAffinity is {expectedAffinity?.ToString() ?? "NONE"}", monster.ElementalAffinity == expectedAffinity);
            Check($"{floorType}-attuned monster's derived OpposingElement is {expectedOpposing?.ToString() ?? "NONE"}", monster.OpposingElement == expectedOpposing);
            Check($"{floorType}-attuned monster uses FloorAttunedAI", monster.AI is FloorAttunedAI);
        }

        CheckBiome(FloorType.Water, DamageType.Water, DamageType.Fire);
        CheckBiome(FloorType.Ice, DamageType.Ice, DamageType.Fire);
        CheckBiome(FloorType.Fire, DamageType.Fire, DamageType.Water);
        CheckBiome(FloorType.Lava, DamageType.Fire, DamageType.Water);
        CheckBiome(FloorType.Mud, DamageType.Earth, DamageType.Air);
        CheckBiome(FloorType.Sand, DamageType.Earth, DamageType.Air);
        CheckBiome(FloorType.Grass, null, null);
        CheckBiome(FloorType.Swamp, DamageType.Water, DamageType.Fire);
        CheckBiome(FloorType.Ash, null, null);

        Check("Monster.CreateFloorAttuned returns null for FloorType.Normal -- no archetype is attuned to plain floor", Monster.CreateFloorAttuned(0, 0, 1, FloorType.Normal, new Random(1)) == null);
    }

    private static Level BuildSingleRoomLevel(int width, int height)
    {
        var level = new Level(1, width, height);
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                level.Tiles[x, y] = Tile.CreateFloor();
            }
        }
        return level;
    }

    private static void FloorAttunementAttritionOnlyAppliesOffPreferredFloorAndUsesMaxHp()
    {
        var level = BuildSingleRoomLevel(10, 10);
        var monster = Monster.CreateFloorAttuned(3, 3, difficultyLevel: 1, FloorType.Water, new Random(2));
        level.Actors.Add(monster);

        level.Tiles[3, 3].FloorType = FloorType.Water;
        int hpBefore = monster.Health.Current;
        var messages = EnvironmentalFloorEffects.ApplyFloorAttunementAttrition(level);
        Check("A floor-attuned monster standing on its own PreferredFloorType takes no off-floor attrition",
            monster.Health.Current == hpBefore && messages.Count == 0);

        level.Tiles[3, 3].FloorType = FloorType.Normal;
        monster.Health.TakeDamage(monster.Health.Max - 10); // drop CurrentHp well below MaxHp first
        int expectedDamage = Math.Max(1, (int)Math.Ceiling(monster.Health.Max * monster.OffPreferredFloorDamagePercent));
        int hpBeforeAttrition = monster.Health.Current;
        EnvironmentalFloorEffects.ApplyFloorAttunementAttrition(level);
        Check("Off-floor attrition is calculated from MaxHP, not the already-reduced CurrentHP",
            hpBeforeAttrition - monster.Health.Current == expectedDamage);
    }

    /// <summary>Regression for a real bug: "The frost salamander withers away from the ice for 1 damage." leaked the raw damage number instead of a severity word like every other damage message in the game.</summary>
    private static void FloorAttunementAttritionMessageUsesSeverityWordNotRawNumber()
    {
        var level = BuildSingleRoomLevel(10, 10);
        var monster = Monster.CreateFloorAttuned(3, 3, difficultyLevel: 1, FloorType.Water, new Random(4));
        level.Actors.Add(monster);
        level.Tiles[3, 3].FloorType = FloorType.Normal;
        level.Tiles[3, 3].IsVisible = true;

        var messages = EnvironmentalFloorEffects.ApplyFloorAttunementAttrition(level);

        Check("A visible off-preferred-floor monster produces exactly one attrition message", messages.Count == 1);
        Check("The floor attunement attrition message uses a severity word, never the raw damage number",
            messages.Count == 1 && !messages[0].Any(char.IsDigit));
    }

    private static void FloorAttunementAttritionAlwaysAtLeastOneDamage()
    {
        var level = BuildSingleRoomLevel(10, 10);
        var monster = Monster.CreateFloorAttuned(3, 3, difficultyLevel: 1, FloorType.Water, new Random(3));
        monster.Health = new Entities.Components.HealthComponent(1); // tiny MaxHp -- 2.5% would floor to 0 without the minimum
        level.Actors.Add(monster);
        level.Tiles[3, 3].FloorType = FloorType.Normal;

        EnvironmentalFloorEffects.ApplyFloorAttunementAttrition(level);
        Check("A very low-MaxHP floor-attuned monster off its preferred floor still loses at least 1 HP, never 0", monster.Health.Current == 0);
    }

    private static void FloorAttunementNeverAppliesToAnOrdinaryMonster()
    {
        var level = BuildSingleRoomLevel(10, 10);
        Monster ordinary = null;
        for (int seed = 0; seed < 200 && ordinary == null; seed++)
        {
            var candidate = Monster.CreateRandom(3, 3, difficultyLevel: 1, new Random(seed));
            if (!candidate.IsFloorAttuned)
            {
                ordinary = candidate;
            }
        }
        Check("An ordinary (non-floor-attuned) monster was found to test against", ordinary != null);
        if (ordinary == null)
        {
            return;
        }

        level.Actors.Add(ordinary);
        level.Tiles[3, 3].FloorType = FloorType.Normal;
        int hpOnNormal = ordinary.Health.Current;
        EnvironmentalFloorEffects.ApplyFloorAttunementAttrition(level);
        Check("An ordinary monster on Normal floor takes no floor-attunement attrition", ordinary.Health.Current == hpOnNormal);

        level.Tiles[3, 3].FloorType = FloorType.Water;
        int hpOnWater = ordinary.Health.Current;
        EnvironmentalFloorEffects.ApplyFloorAttunementAttrition(level);
        Check("An ordinary monster standing on Water (not its terrain -- it has none) still takes no floor-attunement attrition, since it was never floor-attuned to begin with",
            ordinary.Health.Current == hpOnWater);
    }

    /// <summary>
    /// Replaces the old special "elemental terrain multiplier" test -- that mechanic no longer
    /// exists as its own thing (Resistance System spec section 37: terrain protection is now
    /// represented purely as an environmental resistance modifier, see
    /// Monster.GetEffectiveResistance). The exact multiplier equivalence this preserves is
    /// documented on FloorAttunementConfig.OpposingElementBaseResistance's own doc comment.
    /// </summary>
    private static void FloorAttunedMonsterResistanceSwingsWithTerrain()
    {
        var waterImp = Monster.CreateFloorAttuned(0, 0, 1, FloorType.Water, new Random(4));
        Check("Setup: the sampled Water-attuned monster opposes Fire", waterImp.OpposingElement == DamageType.Fire);

        Check("On its own preferred floor, a floor-attuned monster's resistance to its opposing element is strongly positive",
            waterImp.GetEffectiveResistance(ResistanceType.Fire, FloorType.Water)
                == FloorAttunementConfig.OpposingElementBaseResistance + FloorAttunementConfig.PreferredFloorEnvironmentalResistanceBonus);
        Check("Off its preferred floor, the same monster's resistance to its opposing element is strongly negative",
            waterImp.GetEffectiveResistance(ResistanceType.Fire, FloorType.Normal) == FloorAttunementConfig.OpposingElementBaseResistance);
        Check("A resistance type other than the monster's own OpposingElement is never affected by floor-attunement",
            waterImp.GetEffectiveResistance(ResistanceType.Shock, FloorType.Normal) == 0);

        Monster ordinary = null;
        for (int seed = 0; seed < 200 && ordinary == null; seed++)
        {
            var candidate = Monster.CreateRandom(0, 0, 1, new Random(seed));
            if (!candidate.IsFloorAttuned)
            {
                ordinary = candidate;
            }
        }
        Check("An ordinary (non-floor-attuned, no BaseResistances) monster's effective resistance is always 0, regardless of floor",
            ordinary != null && ordinary.GetEffectiveResistance(ResistanceType.Fire, FloorType.Water) == 0);
    }

    /// <summary>
    /// Constructs a small level with a direct 4-step Normal corridor (cost 4*5=20) alongside a
    /// longer 8-step Water detour (cost 7*1 + 1*5 = 12, since the final step onto the Normal
    /// goal tile itself always costs NonPreferredFloorPathCost regardless of route) -- the
    /// detour is cheaper overall despite being longer in tile count, so a Water-attuned
    /// monster's first step should enter the detour rather than the shorter direct corridor.
    /// See design spec section 12/45's own "Preferred Path" test case.
    /// </summary>
    private static void TerrainPathfinderPrefersLongerPreferredFloorRouteOverShorterNormalRoute()
    {
        var level = BuildSingleRoomLevel(7, 6);
        // Direct corridor: (1,1) S -> (2,1) -> (3,1) -> (4,1) -> (5,1) G, all Normal (default FloorType).
        // Water detour: (1,1) -> (1,2) -> (1,3) -> (2,3) -> (3,3) -> (4,3) -> (5,3) -> (5,2) -> (5,1).
        foreach (var (x, y) in new[] { (1, 2), (1, 3), (2, 3), (3, 3), (4, 3), (5, 3), (5, 2) })
        {
            level.Tiles[x, y].FloorType = FloorType.Water;
        }

        var step = TerrainPathfinder.FindNextStep(level, (1, 1), (5, 1), FloorType.Water);
        Check("A Water-attuned monster's cheapest route takes the longer Water detour instead of the shorter all-Normal corridor",
            step == (1, 2));
    }

    private static void TerrainPathfinderFindsNearestReachablePreferredTile()
    {
        var level = BuildSingleRoomLevel(10, 10);
        level.Tiles[6, 6].FloorType = FloorType.Water;
        level.Tiles[8, 8].FloorType = FloorType.Water; // farther -- (6,6) should win

        var nearest = TerrainPathfinder.FindNearestTileOfType(level, (5, 5), FloorType.Water);
        Check("FindNearestTileOfType returns the geometrically closer of two reachable candidates", nearest == (6, 6));

        var none = TerrainPathfinder.FindNearestTileOfType(level, (5, 5), FloorType.Lava);
        Check("FindNearestTileOfType returns null when no tile of that FloorType exists anywhere reachable", none == null);
    }

    private static void FloorAttunedMonstersOnlySpawnOnTheirOwnPreferredFloorType()
    {
        int attunedMonstersChecked = 0;
        bool anyMismatch = false;

        for (int seed = 0; seed < 100; seed++)
        {
            var rng = new Random(seed);
            var level = DungeonGenerator.Generate(1, 60, 30, rng, difficultyLevel: 10, forceTraderSpawn: false);

            foreach (var monster in level.Actors.OfType<Monster>().Where(m => m.IsFloorAttuned))
            {
                attunedMonstersChecked++;
                if (level.Tiles[monster.X, monster.Y].FloorType != monster.PreferredFloorType)
                {
                    anyMismatch = true;
                }
            }
        }

        Check("Floor-attuned monsters were actually spawned across the sampled seeds (test is exercising something)", attunedMonstersChecked > 0);
        Check("Every floor-attuned monster spawns standing on a tile matching its own PreferredFloorType, never elsewhere in the room",
            !anyMismatch);
    }

    private static void FloorAttunedMonsterNeverSelectedWhenItsFloorTypeIsAbsentFromTheLevel()
    {
        bool anyViolation = false;
        int levelsChecked = 0;

        for (int seed = 0; seed < 100; seed++)
        {
            var rng = new Random(seed);
            var level = DungeonGenerator.Generate(1, 60, 30, rng, difficultyLevel: 10, forceTraderSpawn: false);
            levelsChecked++;

            var floorTypesPresent = new HashSet<FloorType>();
            for (int x = 0; x < level.Width; x++)
            {
                for (int y = 0; y < level.Height; y++)
                {
                    if (level.Tiles[x, y].Type == TileType.Floor)
                    {
                        floorTypesPresent.Add(level.Tiles[x, y].FloorType);
                    }
                }
            }

            foreach (var monster in level.Actors.OfType<Monster>().Where(m => m.IsFloorAttuned))
            {
                if (!floorTypesPresent.Contains(monster.PreferredFloorType.Value))
                {
                    anyViolation = true;
                }
            }
        }

        Check("Levels were actually sampled (test is exercising something)", levelsChecked > 0);
        Check("No floor-attuned monster ever spawns whose PreferredFloorType doesn't exist anywhere on that level",
            !anyViolation);
    }

    private static void DeathFromFloorAttunementAttritionUsesNormalDeathPipeline()
    {
        var rng = new Random(6);
        var player = new Player("AttritionDeathTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;

        var monster = Monster.CreateFloorAttuned(player.X + 1, player.Y, difficultyLevel: 1, FloorType.Water, new Random(7));
        monster.Health = new Entities.Components.HealthComponent(1);
        level.Tiles[monster.X, monster.Y].FloorType = FloorType.Normal; // off its preferred floor -- guarantees lethal attrition below
        level.Actors.Add(monster);
        level.Scheduler.Register(monster);

        long goldBefore = player.Gold;
        EnvironmentalFloorEffects.ApplyFloorAttunementAttrition(level);
        Check("Lethal off-floor attrition actually killed the monster", !monster.IsAlive);

        gameLoop.AwardDeathRewards(level);
        level.RemoveDeadActors();
        Check("A monster killed by floor-attunement attrition is removed via the normal death pipeline and awards gold like any other death",
            !level.Actors.Contains(monster) && player.Gold >= goldBefore);
    }

    private static DeadCharacterRecord MakeDeathRecord(int floorReached, DateTime diedAtUtc) => new()
    {
        Name = "Tester",
        ClassName = "Warrior",
        RaceName = "Human",
        Level = 1,
        TurnCount = 10,
        FloorReached = floorReached,
        DiedAtUtc = diedAtUtc,
        CauseOfDeath = "test"
    };

    private static void GraveyardKeepsOnlyTheTenDeepestDeaths()
    {
        var now = DateTime.UtcNow;
        // 15 records, floors 1-15 -- the 10 deepest (6-15) should survive.
        var records = Enumerable.Range(1, 15).Select(floor => MakeDeathRecord(floor, now)).ToList();

        var trimmed = GraveyardManager.TrimToDeepest(records);

        Check("Only the top 10 (MaxEntries) deaths are kept", trimmed.Count == GraveyardManager.MaxEntries);
        Check("The kept deaths are exactly the 10 deepest (floors 6-15)",
            trimmed.Select(r => r.FloorReached).OrderBy(f => f).SequenceEqual(Enumerable.Range(6, 10)));
        Check("The list is ordered deepest floor first", trimmed[0].FloorReached == 15 && trimmed[^1].FloorReached == 6);
    }

    private static void GraveyardTiesBrokenByMostRecentDeath()
    {
        var older = MakeDeathRecord(5, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = MakeDeathRecord(5, new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc));

        var trimmed = GraveyardManager.TrimToDeepest(new List<DeadCharacterRecord> { older, newer });

        Check("Two deaths on the same floor keep the more recent one first",
            trimmed.Count == 2 && trimmed[0] == newer && trimmed[1] == older);
    }

    // --- Resistance System ------------------------------------------------------------

    private static Player MakePlayerWithFixedStats(string name, CharacterClass characterClass, Race race, int constitution, int wisdom)
    {
        var stats = new CharacterStats();
        stats.Get(PrimaryAttribute.Constitution).BaseValue = constitution;
        stats.Get(PrimaryAttribute.Wisdom).BaseValue = wisdom;
        return new Player(name, characterClass, race, stats);
    }

    /// <summary>Design spec section 68: Human/Warrior/CON10/WIS10/no equipment -- every resistance should equal exactly Race + Class (both 0 stat contribution at CON/WIS 10).</summary>
    private static void ResistanceBaseValuesMatchRaceClassStatTable()
    {
        var player = MakePlayerWithFixedStats("ResistBaseTester", CharacterClass.Warrior, Race.Human, constitution: 10, wisdom: 10);

        Check("Human Warrior at CON/WIS 10 with no equipment: Fire resistance is exactly Class's +3 (Race +0, CON stat +0)",
            player.GetEffectiveResistance(ResistanceType.Fire) == 3);
        Check("...Water resistance is exactly 0 (Race +0, Class +0)", player.GetEffectiveResistance(ResistanceType.Water) == 0);
        Check("...Ice resistance is exactly Class's +3", player.GetEffectiveResistance(ResistanceType.Ice) == 3);
        Check("...Shock resistance is exactly 0", player.GetEffectiveResistance(ResistanceType.Shock) == 0);
        Check("...Poison resistance is exactly Class's +3", player.GetEffectiveResistance(ResistanceType.Poison) == 3);
        Check("...Magic resistance is exactly 0 (WIS stat +0, Class +0)", player.GetEffectiveResistance(ResistanceType.Magic) == 0);
    }

    private static void ResistanceConstitutionModifierTableMatchesSpec()
    {
        Check("CON 3 -> -6", ResistanceStatModifiers.GetConstitutionModifier(3) == -6);
        Check("CON 10 -> 0", ResistanceStatModifiers.GetConstitutionModifier(10) == 0);
        Check("CON 17 -> +4", ResistanceStatModifiers.GetConstitutionModifier(17) == 4);
        Check("CON 18 -> +6", ResistanceStatModifiers.GetConstitutionModifier(18) == 6);
    }

    private static void ResistanceWisdomModifierTableMatchesSpec()
    {
        Check("WIS 3 -> -6", ResistanceStatModifiers.GetWisdomMagicModifier(3) == -6);
        Check("WIS 10 -> 0", ResistanceStatModifiers.GetWisdomMagicModifier(10) == 0);
        Check("WIS 17 -> +4", ResistanceStatModifiers.GetWisdomMagicModifier(17) == 4);
        Check("WIS 18 -> +6", ResistanceStatModifiers.GetWisdomMagicModifier(18) == 6);
    }

    /// <summary>Design spec section 71: Base Fire = +10 (Priest/Human/CON17, giving CON +4 -- Priest and Human both contribute 0 Fire, so a CON of 17 alone doesn't reach +10; use Warrior instead: Race 0 + Class 3 + CON(17)=4 = 7 -- pick a CON that lands base exactly at +10 instead of re-deriving the spec's own unrelated example character) plus Ring of Embers (+15) = +25 effective.</summary>
    private static void ResistanceEquipmentAddsToEffectiveTotal()
    {
        // Warrior/Human, CON 18 (+6 stat) -> base Fire = Race(0) + Class(3) + CON(6) = 9.
        var player = MakePlayerWithFixedStats("ResistEquipTester", CharacterClass.Warrior, Race.Human, constitution: 18, wisdom: 10);
        int baseline = player.GetEffectiveResistance(ResistanceType.Fire);
        Check("Baseline Fire resistance before equipping anything", baseline == 9);

        player.Equipment.EquipInSlot(EquipmentSlot.PrimaryRing, Items.RingOfEmbers.Clone());
        Check("Equipping Ring of Embers (+15 Fire) adds directly to the effective total",
            player.GetEffectiveResistance(ResistanceType.Fire) == baseline + 15);
    }

    /// <summary>Design spec section 72: cursed equipment subtracts the same way positive equipment adds.</summary>
    private static void ResistanceCursedEquipmentSubtractsFromEffectiveTotal()
    {
        var player = MakePlayerWithFixedStats("ResistCurseTester", CharacterClass.Warrior, Race.Human, constitution: 18, wisdom: 10);
        int baseline = player.GetEffectiveResistance(ResistanceType.Fire);

        player.Equipment.EquipInSlot(EquipmentSlot.PrimaryRing, Items.CharredBand.Clone());
        Check("Equipping Charred Band (-20 Fire, cursed) subtracts directly from the effective total",
            player.GetEffectiveResistance(ResistanceType.Fire) == baseline - 20);
    }

    /// <summary>Design spec section 73: several equipped items (positive and cursed) all stack additively into one effective total.</summary>
    private static void ResistanceMultipleItemsStackAdditively()
    {
        var player = MakePlayerWithFixedStats("ResistStackTester", CharacterClass.Thief, Race.Human, constitution: 10, wisdom: 10);
        int baseline = player.GetEffectiveResistance(ResistanceType.Fire);
        Check("Baseline Fire resistance for a Thief (Race 0, Class 0, CON 0) is exactly 0", baseline == 0);

        player.Equipment.EquipInSlot(EquipmentSlot.PrimaryRing, Items.RingOfEmbers.Clone());
        player.Equipment.EquipInSlot(EquipmentSlot.Feet, Items.SalamanderBoots.Clone());
        player.Equipment.EquipInSlot(EquipmentSlot.OffHandRing, Items.CharredBand.Clone());

        Check("Ring of Embers (+15) + Salamander Boots (+8) + Charred Band (-20) = +3 total",
            player.GetEffectiveResistance(ResistanceType.Fire) == baseline + 15 + 8 - 20);
    }

    /// <summary>Design spec section 74: an extreme total is clamped to ResistanceConfig's Min/Max, never left unbounded.</summary>
    private static void ResistanceClampsToConfiguredRange()
    {
        Check("A calculated total of +95 clamps to ResistanceConfig.MaximumResistance (+75)",
            Math.Clamp(95, ResistanceConfig.MinimumResistance, ResistanceConfig.MaximumResistance) == ResistanceConfig.MaximumResistance);
        Check("A calculated total of -100 clamps to ResistanceConfig.MinimumResistance (-75)",
            Math.Clamp(-100, ResistanceConfig.MinimumResistance, ResistanceConfig.MaximumResistance) == ResistanceConfig.MinimumResistance);

        // A real, reachable clamp case via GetEffectiveResistance itself: several strongly-negative
        // sources on the same type, summing well past -75.
        var player = MakePlayerWithFixedStats("ResistClampLowTester", CharacterClass.Warrior, Race.Human, constitution: 10, wisdom: 10);
        player.Equipment.EquipInSlot(EquipmentSlot.PrimaryRing, Items.CharredBand.Clone());
        player.Equipment.EquipInSlot(EquipmentSlot.OffHandRing, Items.CharredBand.Clone());
        player.Equipment.EquipInSlot(EquipmentSlot.Head, Items.AshenCrown.Clone());
        // Class Warrior +3, then -20 -20 -20 = -57 raw -- still above -75, so add one more source via an ActiveEffect debuff to actually breach the floor.
        player.ActiveEffects.Add(new ActiveEffect("Test Curse of Flame", int.MaxValue) { ModifiedResistanceType = ResistanceType.Fire, ResistanceAmount = -30 });
        Check("A sufficiently negative combined total clamps to ResistanceConfig.MinimumResistance (-75), never goes lower",
            player.GetEffectiveResistance(ResistanceType.Fire) == ResistanceConfig.MinimumResistance);
    }

    /// <summary>Design spec section 75.</summary>
    private static void ResistanceReducesIncomingDamage()
    {
        var level = new Level(1, 5, 5);
        for (int x = 0; x < 5; x++) for (int y = 0; y < 5; y++) level.Tiles[x, y] = Tile.CreateFloor();
        var player = MakePlayerWithFixedStats("ResistDamageTester", CharacterClass.Thief, Race.Human, constitution: 10, wisdom: 10);
        player.X = 2; player.Y = 2;
        player.ActiveEffects.Add(new ActiveEffect("Test Resist Fire", int.MaxValue) { ModifiedResistanceType = ResistanceType.Fire, ResistanceAmount = 25 });

        int result = FloorDamageCalculator.ApplyFloorMultiplier(level, player, DamageType.Fire, 100);
        Check("100 Fire damage at +25 Fire resistance becomes 75", result == 75);
    }

    /// <summary>Design spec section 76.</summary>
    private static void NegativeResistanceIncreasesIncomingDamage()
    {
        var level = new Level(1, 5, 5);
        for (int x = 0; x < 5; x++) for (int y = 0; y < 5; y++) level.Tiles[x, y] = Tile.CreateFloor();
        var player = MakePlayerWithFixedStats("VulnDamageTester", CharacterClass.Thief, Race.Human, constitution: 10, wisdom: 10);
        player.X = 2; player.Y = 2;
        player.ActiveEffects.Add(new ActiveEffect("Test Curse of Flame", int.MaxValue) { ModifiedResistanceType = ResistanceType.Fire, ResistanceAmount = -25 });

        int result = FloorDamageCalculator.ApplyFloorMultiplier(level, player, DamageType.Fire, 100);
        Check("100 Fire damage at -25 Fire resistance becomes 125", result == 125);
    }

    /// <summary>Design spec section 77.</summary>
    private static void ResistanceReducesStatusApplicationChance()
    {
        var level = new Level(1, 5, 5);
        var player = MakePlayerWithFixedStats("StatusChanceTester", CharacterClass.Thief, Race.Human, constitution: 10, wisdom: 10);
        player.ActiveEffects.Add(new ActiveEffect("Test Resist Fire", int.MaxValue) { ModifiedResistanceType = ResistanceType.Fire, ResistanceAmount = 20 });

        double adjusted = ResistanceCalculator.AdjustStatusChance(level, player, DamageType.Fire, 0.50);
        Check("50% base Burn chance at +20 Fire resistance becomes 40%", Math.Abs(adjusted - 0.40) < 0.0001);
    }

    /// <summary>Design spec section 78: duration uses only StatusDurationResistanceFactor (50%) of the resistance value.</summary>
    private static void ResistanceReducesStatusDurationAtHalfStrength()
    {
        var level = new Level(1, 5, 5);
        var player = MakePlayerWithFixedStats("StatusDurationTester", CharacterClass.Thief, Race.Human, constitution: 10, wisdom: 10);
        player.ActiveEffects.Add(new ActiveEffect("Test Resist Fire", int.MaxValue) { ModifiedResistanceType = ResistanceType.Fire, ResistanceAmount = 20 });

        int adjusted = ResistanceCalculator.AdjustStatusDuration(level, player, DamageType.Fire, 10);
        Check("A 10-turn Burn at +20 Fire resistance (10% effective duration reduction) becomes 9 turns", adjusted == 9);
    }

    /// <summary>Design spec section 80: Magic Resistance reduces Arcane damage but must NOT touch Fire/Ice/Shock, even on the same target.</summary>
    private static void MagicResistanceAffectsArcaneNotElementalSpells()
    {
        var level = new Level(1, 5, 5);
        for (int x = 0; x < 5; x++) for (int y = 0; y < 5; y++) level.Tiles[x, y] = Tile.CreateFloor();
        var player = MakePlayerWithFixedStats("MagicResistTester", CharacterClass.Thief, Race.Human, constitution: 10, wisdom: 10);
        player.X = 2; player.Y = 2;
        player.ActiveEffects.Add(new ActiveEffect("Test Warding", int.MaxValue) { ModifiedResistanceType = ResistanceType.Magic, ResistanceAmount = 50 });

        int magicMissileDamage = FloorDamageCalculator.ApplyFloorMultiplier(level, player, DamageType.Arcane, 40);
        int fireballDamage = FloorDamageCalculator.ApplyFloorMultiplier(level, player, DamageType.Fire, 40);
        Check("50 Magic resistance halves an Arcane (Magic Missile-type) hit", magicMissileDamage == 20);
        Check("The same Magic resistance does nothing at all to a Fire (Fireball-type) hit on the same target", fireballDamage == 40);
    }

    /// <summary>Design spec section 33/34/81: the floor's own elemental multiplier and the target's resistance both apply, in sequence.</summary>
    private static void FloorDamageCalculatorComposesFloorMultiplierWithResistance()
    {
        var level = new Level(1, 5, 5);
        for (int x = 0; x < 5; x++) for (int y = 0; y < 5; y++) level.Tiles[x, y] = Tile.CreateFloor();
        level.Tiles[2, 2].FloorType = FloorType.Water; // FireDamageMultiplier 0.5
        var player = MakePlayerWithFixedStats("FloorResistComboTester", CharacterClass.Thief, Race.Human, constitution: 10, wisdom: 10);
        player.X = 2; player.Y = 2;
        player.ActiveEffects.Add(new ActiveEffect("Test Resist Fire", int.MaxValue) { ModifiedResistanceType = ResistanceType.Fire, ResistanceAmount = 25 });

        // 40 Fireball -> Water floor halves to 20 -> 25 resistance reduces further to 15.
        int result = FloorDamageCalculator.ApplyFloorMultiplier(level, player, DamageType.Fire, 40);
        Check("Floor multiplier (Water halves Fire) and resistance (25%) both apply in sequence: 40 -> 20 -> 15", result == 15);
    }

    /// <summary>
    /// Design spec section 37/85: the tile-attuned monster system's old special elemental-terrain
    /// multiplier no longer exists as a second mechanism -- verified by isolating the case where
    /// double-application would be detectable. Ice carries no floor-of-its-own elemental
    /// multiplier (unlike Water/Fire/Lava), so an Ice-attuned monster standing on Ice isolates
    /// the environmental resistance bonus alone: if the old multiplier were still active
    /// alongside it, this would come out to 10 (40 * 0.5 * 0.5), not 20 (40 * 0.5).
    /// </summary>
    private static void TileAttunedMonsterNeverDoubleAppliesResistance()
    {
        var level = new Level(1, 5, 5);
        for (int x = 0; x < 5; x++) for (int y = 0; y < 5; y++) level.Tiles[x, y] = Tile.CreateFloor();
        var iceImp = Monster.CreateFloorAttuned(2, 2, 1, FloorType.Ice, new Random(8));
        Check("Setup: the sampled Ice-attuned monster opposes Fire", iceImp.OpposingElement == DamageType.Fire);
        level.Actors.Add(iceImp);
        level.Tiles[2, 2].FloorType = FloorType.Ice;

        int result = FloorDamageCalculator.ApplyFloorMultiplier(level, iceImp, DamageType.Fire, 40);
        Check("An Ice-attuned monster on Ice takes exactly the single resistance-based reduction (40 * 0.5 = 20), not a doubled-up 10",
            result == 20);
    }

    /// <summary>Design spec section 41 -- the breakdown API isn't shown on the compact inventory line, but every source it lists should sum to exactly the same value GetEffectiveResistance returns.</summary>
    private static void ResistanceBreakdownSumsToEffectiveValue()
    {
        var player = MakePlayerWithFixedStats("BreakdownTester", CharacterClass.Warrior, Race.Dwarf, constitution: 17, wisdom: 10);
        player.Equipment.EquipInSlot(EquipmentSlot.PrimaryRing, Items.RingOfEmbers.Clone());
        player.Equipment.EquipInSlot(EquipmentSlot.Head, Items.AshenCrown.Clone());

        var breakdown = player.GetResistanceBreakdown(ResistanceType.Fire);
        int sumOfSources = breakdown.Where(x => x.Source != "Effective").Sum(x => x.Amount);
        int effective = breakdown.First(x => x.Source == "Effective").Amount;

        Check("Every named source in the breakdown sums to the same total GetEffectiveResistance returns",
            Math.Clamp(sumOfSources, ResistanceConfig.MinimumResistance, ResistanceConfig.MaximumResistance) == effective
            && effective == player.GetEffectiveResistance(ResistanceType.Fire));
        Check("The breakdown's Equipment line reflects Ring of Embers (+15), separate from the Curse line",
            breakdown.First(x => x.Source == "Equipment").Amount == 15);
        Check("The breakdown's Curse line reflects Ashen Crown (-20 Fire), separate from ordinary Equipment",
            breakdown.First(x => x.Source == "Curse").Amount == -20);
    }

    private static void InventoryResistLineFormatMatchesSpec()
    {
        var player = MakePlayerWithFixedStats("ResistFormatTester", CharacterClass.Warrior, Race.Dwarf, constitution: 10, wisdom: 10);
        // Dwarf Fire +3, Class Warrior Fire +3 -> +6 (nonzero, positive). Water stays exactly 0 (bare "0", no sign).
        string line = InventoryScreen.FormatResistLine(player);
        Check("Resist line shows a bare \"0\" for exactly-zero Water resistance", line.Contains("Wat 0"));
        Check("Resist line shows an explicit \"+\" sign for positive Fire resistance", line.Contains("Fir +6"));
    }

    private static void InventoryResistLineRefreshesOnEquipAndUnequip()
    {
        var rng = new Random(9);
        var player = new Player("ResistRefreshTester", CharacterClass.Thief, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Thief, rng));
        player.Level = 3; // Ring of Embers has a minimumLevel of 3
        var level = new GameLoop(player).CurrentLevel;
        int before = player.GetEffectiveResistance(ResistanceType.Fire);

        var ring = Items.RingOfEmbers.Clone();
        player.Inventory.AddItem(ring);
        InventoryScreen.ApplyItem(player, ring, level);
        Check("Equipping Ring of Embers immediately raises effective Fire resistance by +15",
            player.GetEffectiveResistance(ResistanceType.Fire) == before + 15);

        player.Equipment.Unequip(EquipmentSlot.PrimaryRing);
        Check("Unequipping it returns effective Fire resistance to its prior value",
            player.GetEffectiveResistance(ResistanceType.Fire) == before);
    }

    // --- Ambient Sound / Hearing System -------------------------------------------------

    private class AlwaysSucceedRandom : Random
    {
        public override double NextDouble() => 0.0;
    }

    private static Level BuildOpenSoundTestLevel(int width = 20, int height = 20)
    {
        var level = new Level(1, width, height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                level.Tiles[x, y] = Tile.CreateFloor();
            }
        }
        return level;
    }

    /// <summary>
    /// Scans the roster for an archetype with the requested SoundType. CreateRandom's "closest
    /// 6 archetypes to this floor's target difficulty" selection means a fixed difficultyLevel
    /// only ever reaches a narrow DifficultyRating band -- varying difficultyLevel across the
    /// full spectrum (not just the RNG seed) is what actually guarantees every archetype,
    /// wherever its own rating falls, eventually becomes reachable.
    /// </summary>
    private static Monster FindMonsterWithSoundType(MonsterSoundType desired, int x, int y)
    {
        for (int difficulty = 1; difficulty <= 80; difficulty++)
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var monster = Monster.CreateRandom(x, y, difficulty, new Random((difficulty * 1000) + seed));
                if (monster.SoundType == desired)
                {
                    return monster;
                }
            }
        }
        return null;
    }

    private static void SoundCooldownBlocksFurtherSoundsUntilItExpires()
    {
        var level = BuildOpenSoundTestLevel();
        var player = MakePlayerWithFixedStats("SoundCooldownTester", CharacterClass.Warrior, Race.Human, constitution: 10, wisdom: 10);
        level.Actors.Add(FindMonsterWithSoundType(MonsterSoundType.Howl, 1, 1));

        var rng = new AlwaysSucceedRandom();
        string first = SoundSystem.ProcessTurn(level, player, rng);
        Check("A rigged always-succeed roll produces a sound message", first != null);
        Check("TurnsSinceLastSound resets to 0 immediately after a successful sound", level.SoundState.TurnsSinceLastSound == 0);
        Check("A new cooldown is set after a successful sound", level.SoundState.SoundCooldownRemaining > 0);

        int cooldown = level.SoundState.SoundCooldownRemaining;
        bool anySoundDuringCooldown = false;
        for (int i = 0; i < cooldown; i++)
        {
            if (SoundSystem.ProcessTurn(level, player, rng) != null)
            {
                anySoundDuringCooldown = true;
            }
        }
        Check("No ordinary ambient sound occurs during the cooldown window, even with a guaranteed-success RNG",
            !anySoundDuringCooldown);
    }

    private static void SoundChanceIncreasesWithLongerSilence()
    {
        var player = MakePlayerWithFixedStats("SoundSilenceTester", CharacterClass.Warrior, Race.Human, constitution: 10, wisdom: 10);
        var definition = MonsterSoundCatalog.Get(MonsterSoundType.Howl);

        double chanceAt10 = SoundSystem.CalculateSoundChance(definition, player, null, new DungeonSoundState { TurnsSinceLastSound = 10 });
        double chanceAt70 = SoundSystem.CalculateSoundChance(definition, player, null, new DungeonSoundState { TurnsSinceLastSound = 70 });
        Check("70 turns of silence yields a strictly higher chance than 10 turns, all else equal", chanceAt70 > chanceAt10);
    }

    private static void SoundChanceNeverGuaranteedEvenAfterExtremeSilence()
    {
        var player = MakePlayerWithFixedStats("SoundNoGuaranteeTester", CharacterClass.Warrior, Race.Human, constitution: 10, wisdom: 10);
        var definition = MonsterSoundCatalog.Get(MonsterSoundType.Howl);

        double chance = SoundSystem.CalculateSoundChance(definition, player, null, new DungeonSoundState { TurnsSinceLastSound = 1_000_000 });
        Check("Chance clamps to MaximumAmbientSoundChance and never reaches 100%, however long the silence",
            chance == SoundConfig.MaximumAmbientSoundChance && chance < 1.0);
    }

    private static void WisdomModestlyShiftsHearingChanceWithoutExtremes()
    {
        var lowWisdom = MakePlayerWithFixedStats("LowWisHearingTester", CharacterClass.Warrior, Race.Human, constitution: 10, wisdom: 3);
        var highWisdom = MakePlayerWithFixedStats("HighWisHearingTester", CharacterClass.Warrior, Race.Human, constitution: 10, wisdom: 18);
        var definition = MonsterSoundCatalog.Get(MonsterSoundType.Howl);
        var state = new DungeonSoundState { TurnsSinceLastSound = 25 };

        double lowChance = SoundSystem.CalculateSoundChance(definition, lowWisdom, 6, state);
        double highChance = SoundSystem.CalculateSoundChance(definition, highWisdom, 6, state);
        Check("A high-WIS character has a modestly higher hearing chance than a low-WIS character", highChance > lowChance);
        Check("Neither character's chance is ever 0% or 100%", lowChance > 0.0 && highChance < 1.0);
    }

    private static void SoundDistanceRespectsMaxRange()
    {
        var level = BuildOpenSoundTestLevel(30, 30);
        bool reachableAt13 = SoundDistance.TryGetDistance(level, (0, 0), (13, 0), 12, out _);
        Check("A source 13 tiles away with MaxRange 12 cannot be reached", !reachableAt13);

        bool reachableAt10 = SoundDistance.TryGetDistance(level, (0, 0), (10, 0), 12, out int distanceAt10);
        Check("A source 10 tiles away with MaxRange 12 can be reached, at its true distance", reachableAt10 && distanceAt10 == 10);
    }

    private static void CloserSoundSourceHasHigherChanceThanFarther()
    {
        var player = MakePlayerWithFixedStats("SoundDistanceChanceTester", CharacterClass.Warrior, Race.Human, constitution: 10, wisdom: 10);
        var definition = MonsterSoundCatalog.Get(MonsterSoundType.Howl);
        var state = new DungeonSoundState();

        double nearChance = SoundSystem.CalculateSoundChance(definition, player, 2, state);
        double farChance = SoundSystem.CalculateSoundChance(definition, player, 14, state);
        Check("A closer sound source yields a strictly higher chance than a farther one", nearChance > farChance);
    }

    private static void CreatureAmbientPoolReflectsLivingMonsters()
    {
        var level = BuildOpenSoundTestLevel();
        level.Actors.Add(FindMonsterWithSoundType(MonsterSoundType.Wings, 1, 1));
        level.Actors.Add(FindMonsterWithSoundType(MonsterSoundType.Howl, 2, 2));

        var pool = SoundSystem.GetCreatureAmbientSoundTypes(level);
        Check("The level's creature ambient pool includes Wings (a living Giant Bat is present)", pool.Contains(MonsterSoundType.Wings));
        Check("The level's creature ambient pool includes Howl (a living Dire Wolf is present)", pool.Contains(MonsterSoundType.Howl));
    }

    private static void DuplicateMonsterSoundTypesAppearOnlyOnce()
    {
        var level = BuildOpenSoundTestLevel();
        for (int i = 0; i < 5; i++)
        {
            level.Actors.Add(FindMonsterWithSoundType(MonsterSoundType.Wings, i + 1, 1));
        }

        var pool = SoundSystem.GetCreatureAmbientSoundTypes(level);
        Check("5 identical-SoundType monsters contribute exactly one pool entry, not five", pool.Count(t => t == MonsterSoundType.Wings) == 1);
    }

    private static void SilentMonsterNeverContributesAmbientSound()
    {
        var level = BuildOpenSoundTestLevel();
        var silentMonster = FindMonsterWithSoundType(MonsterSoundType.None, 1, 1);
        Check("Setup: a None-SoundType archetype was actually found", silentMonster != null);
        level.Actors.Add(silentMonster);

        var pool = SoundSystem.GetCreatureAmbientSoundTypes(level);
        Check("A None-SoundType monster never appears in the ambient pool", !pool.Contains(MonsterSoundType.None));
    }

    private static void BossSoundStopsAfterBossDeath()
    {
        var level = BuildOpenSoundTestLevel();
        var player = MakePlayerWithFixedStats("BossSoundTester", CharacterClass.Warrior, Race.Human, constitution: 10, wisdom: 10);
        player.X = 5; player.Y = 5;

        var boss = Monster.CreateBoss(6, 5, 5, new Random(3));
        boss.SoundType = MonsterSoundType.Roar; // guaranteed non-None regardless of which archetype was rolled
        level.Actors.Add(boss);

        var candidatesWhileAlive = SoundSystem.BuildCandidates(level, player, level.SoundState);
        Check("A living boss with a SoundType contributes a Boss candidate",
            candidatesWhileAlive.Any(c => c.Category == SoundCategory.Boss));

        boss.Health.TakeDamage(boss.Health.Max + 100);
        Check("Setup: the boss is actually dead now", !boss.IsAlive);

        var candidatesAfterDeath = SoundSystem.BuildCandidates(level, player, level.SoundState);
        Check("A dead boss no longer contributes a Boss candidate", !candidatesAfterDeath.Any(c => c.Category == SoundCategory.Boss));
    }

    private static void EnvironmentSourceOnlyBecomesCandidateWithinRange()
    {
        var level = BuildOpenSoundTestLevel(30, 30);
        level.AmbientSoundSources.Add(new AmbientSoundSource(20, 20, FloorType.Water)); // Water = Loud = range 12
        var player = MakePlayerWithFixedStats("EnvSoundRangeTester", CharacterClass.Warrior, Race.Human, constitution: 10, wisdom: 10);

        player.X = 0; player.Y = 0; // far outside range
        var farCandidates = SoundSystem.BuildCandidates(level, player, level.SoundState);
        Check("An environment source well outside its Loudness range produces no Environment candidate",
            !farCandidates.Any(c => c.Category == SoundCategory.Environment));

        player.X = 15; player.Y = 20; // 5 tiles away, well within range
        var nearCandidates = SoundSystem.BuildCandidates(level, player, level.SoundState);
        Check("The same environment source within range produces an Environment candidate",
            nearCandidates.Any(c => c.Category == SoundCategory.Environment));
    }

    private static void RecentlyUsedSoundKeysAreAvoidedWhenAlternativesExist()
    {
        var level = BuildOpenSoundTestLevel();
        level.Actors.Add(FindMonsterWithSoundType(MonsterSoundType.Wings, 1, 1));
        level.Actors.Add(FindMonsterWithSoundType(MonsterSoundType.Howl, 2, 2));
        var player = MakePlayerWithFixedStats("RecentSoundTester", CharacterClass.Warrior, Race.Human, constitution: 10, wisdom: 10);
        level.SoundState.RecentSoundKeys.Add("creature:Wings");

        var candidates = SoundSystem.BuildCandidates(level, player, level.SoundState);
        Check("A recently-used sound key is excluded when a valid alternative exists",
            !candidates.Any(c => c.Key == "creature:Wings") && candidates.Any(c => c.Key == "creature:Howl"));

        var soloLevel = BuildOpenSoundTestLevel();
        soloLevel.Actors.Add(FindMonsterWithSoundType(MonsterSoundType.Wings, 1, 1));
        soloLevel.SoundState.RecentSoundKeys.Add("creature:Wings");
        var soloCandidates = SoundSystem.BuildCandidates(soloLevel, player, soloLevel.SoundState);
        Check("A recently-used sound key is still included as a fallback when it's the only candidate available",
            soloCandidates.Any(c => c.Key == "creature:Wings"));
    }

    private static void SoundSystemAdvancesSilenceTimerExactlyOncePerCall()
    {
        // Each call to SoundSystem.ProcessTurn corresponds to exactly one completed normal
        // player turn (move/wait/cast/fire/throw all funnel through the same single call site
        // in GameLoop.Run -- see SoundSystem's own doc comment), so a fired projectile's
        // multi-tile travel animation -- which happens entirely INSIDE one such call, never as
        // extra calls of its own -- can never advance this timer more than once per real turn.
        // No sound sources of any kind on this level (no ambient sources, no boss, no
        // monsters), so BuildCandidates is always empty and ProcessTurn can never succeed --
        // TurnsSinceLastSound is guaranteed to climb by exactly 1 per call, deterministically.
        var level = BuildOpenSoundTestLevel();
        var player = MakePlayerWithFixedStats("SilenceTimerTester", CharacterClass.Warrior, Race.Human, constitution: 10, wisdom: 10);
        var rng = new Random(4);

        for (int i = 1; i <= 10; i++)
        {
            string message = SoundSystem.ProcessTurn(level, player, rng);
            Check($"Call #{i} with no sound sources on the level produces no message", message == null);
            Check($"TurnsSinceLastSound reflects exactly {i} calls so far", level.SoundState.TurnsSinceLastSound == i);
        }
    }

    private static void ItemUsabilityAccountsForClassEquipSpellAndThrowRestrictions()
    {
        var rng = new Random(80);
        var warrior = new Player("UsabilityWarrior", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var spellbook = new Item("Test Spellbook of Smite", '+', "test", ItemType.Spellbook, teachesSpell: SpellCatalog.Heal);

        Check("A Warrior cannot use a spellbook that teaches a spell their class could never cast (the reported bug)",
            !ItemRequirementValidator.CanUse(warrior, spellbook, out _));

        Check("A Warrior cannot use (equip) a Wand -- Wand isn't in Warrior.AllowedWeaponTypes",
            !ItemRequirementValidator.CanUse(warrior, Items.BasicWand, out _));

        var thief = new Player("UsabilityThief", CharacterClass.Thief, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Thief, rng));
        Check("A Thief can use a Throwing Axe by throwing it, even though they can't equip an Axe in melee",
            ItemRequirementValidator.CanUse(thief, Items.ThrowingAxe, out _));

        var mage = new Player("UsabilityMage", CharacterClass.Mage, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Mage, rng));
        Check("A Mage cannot use a Throwing Axe at all -- can't equip it, and isn't trained to throw Martial items",
            !ItemRequirementValidator.CanUse(mage, Items.ThrowingAxe, out _));

        Check("A Scroll of Identify (no class restriction, not equipment, not throwable) stays usable by any class",
            ItemRequirementValidator.CanUse(warrior, Items.ScrollOfIdentify, out _));

        // Found during a consistency audit: a Wand of Fire is cast from inventory via its own
        // Charges (GameLoop.HandleCastSpell's wand branch, SpellRequirementValidator's own doc
        // comment) with no class/equip check at all -- so it must never be marked "unusable"
        // just because this class can't equip a Wand, unlike a purely stat-bonus wand (BasicWand)
        // which really does need equipping to do anything.
        Check("A Warrior can use a Wand of Fire (cast from its charges) even though they could never equip a Wand",
            ItemRequirementValidator.CanUse(warrior, Items.WandOfFire, out _));
        Check("A Warrior genuinely cannot use a plain stat-bonus wand with no CastsSpell -- equipping is the only thing it does",
            !ItemRequirementValidator.CanUse(warrior, Items.BasicWand, out _));
        Check("StartingGearGenerator still excludes a Wand of Fire from a Warrior's random starting weapon (equip-only check, independent of CanUse's charge-cast exception)",
            !EquipmentCompatibility.IsAllowedForClass(CharacterClass.Warrior, Items.WandOfFire, out _));
    }

    private static void MageCanCastIdentifyAndItAlwaysSucceeds()
    {
        Check("Identify is castable by both Priest and Mage (AllSpellcasters), not Priest-only anymore",
            SpellCatalog.Identify.CanBeCastBy(CharacterClass.Mage) && SpellCatalog.Identify.CanBeCastBy(CharacterClass.Priest));

        var rng = new Random(90);
        var level = BuildOpenLevel(5, 5);
        var mage = new Player("IdentifyMage", CharacterClass.Mage, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Mage, rng));
        mage.Mana = new ManaComponent(50);
        var unidentified = Items.HolyArrow.Clone();

        var context = new SpellCastingContext(mage, level, level.TurnNumber, rng) { TargetItem = unidentified };
        var result = SpellCaster.Cast(SpellCatalog.Identify, context);

        Check("Casting Identify as a Mage always succeeds and identifies the target item",
            result.Success && unidentified.IsIdentified && result.IdentifiedItemName == unidentified.Name);
    }

    private static void ThiefIdentifySkillChanceScalesWithKnowledgeStat()
    {
        Check("StatBasedIdentifyEffect's success chance is exactly 50% at the average-roll baseline (stat 10)",
            Math.Abs(StatBasedIdentifyEffect.ChanceForStat(10) - 0.50) < 0.0001);
        Check("Higher Knowledge raises the identify chance and lower Knowledge lowers it, both clamped to a sane band",
            StatBasedIdentifyEffect.ChanceForStat(30) > StatBasedIdentifyEffect.ChanceForStat(10)
            && StatBasedIdentifyEffect.ChanceForStat(-10) < StatBasedIdentifyEffect.ChanceForStat(10)
            && StatBasedIdentifyEffect.ChanceForStat(1000) <= 0.95
            && StatBasedIdentifyEffect.ChanceForStat(-1000) >= 0.10);
    }

    private static int SampleIdentifyAttempts(Player thief, Level level, Random rng, int samples)
    {
        int successes = 0;
        for (int i = 0; i < samples; i++)
        {
            var item = Items.HolyArrow.Clone();
            var context = new SpellCastingContext(thief, level, level.TurnNumber, rng) { TargetItem = item };
            var result = new SpellCastResult();
            new StatBasedIdentifyEffect(PrimaryAttribute.Knowledge).Apply(context, result);
            if (item.IsIdentified)
            {
                successes++;
            }
        }
        return successes;
    }

    private static void ThiefIdentifySkillCanSucceedAndFail()
    {
        var rng = new Random(92);
        var level = BuildOpenLevel(5, 5);
        var thief = new Player("IdentifyThiefWiring", CharacterClass.Thief, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Thief, rng));

        thief.Stats.Get(PrimaryAttribute.Knowledge).EquipmentModifier += 1000; // pin near MaxChance (0.95)
        int highKnowledgeSuccesses = SampleIdentifyAttempts(thief, level, rng, 100);

        thief.Stats.Get(PrimaryAttribute.Knowledge).EquipmentModifier -= 2000; // Adjusted floors at 1 -> near MinChance (0.23 at stat 1)
        int lowKnowledgeSuccesses = SampleIdentifyAttempts(thief, level, rng, 100);

        Check("Higher effective Knowledge makes the Thief's Identify skill succeed far more often than low Knowledge does (~95% vs ~23% over 100 samples)",
            highKnowledgeSuccesses > lowKnowledgeSuccesses + 30);
    }

    /// <summary>
    /// Every duplicate display name in the catalog must belong to the deliberate 4/6/8-slot
    /// container variants (see ContainerCatalog's own doc comment -- slot count is intentionally
    /// left out of a bag's Name since it's shown elsewhere, so e.g. every "Pouch of Lightening"
    /// shares one Name across its three slot-capacity versions; SaveManager.ResolveTemplate
    /// disambiguates them at load time using the saved Container shape). Anything else sharing a
    /// name is the original, unintended Scroll of Identify-style collision this test was written
    /// to catch.
    /// </summary>
    private static void NoDuplicateItemNamesAcrossTheFullCatalogExceptDeliberateContainerVariants()
    {
        var duplicateGroups = Items.All.GroupBy(i => i.Name).Where(g => g.Count() > 1).ToList();

        bool everyDuplicateIsAThreeWayContainerVariant = duplicateGroups.All(g =>
            g.Count() == 3 && g.All(i => i.Type == ItemType.Container));

        Check("Every duplicate display name belongs to the deliberate 4/6/8-slot container variants, never anything else",
            everyDuplicateIsAThreeWayContainerVariant);
    }

    // --- Turn counter and natural regeneration --------------------------------------

    private static void TurnCountStartsAtZero()
    {
        var player = new Player("TurnCountTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(100)));
        Check("A freshly created character starts with TurnCount 0", player.TurnCount == 0);
    }

    /// <summary>
    /// Design targets given directly by the user: originally HP divisors Warrior 46, Thief/
    /// Priest 64, Mage 82 (Mana 46 for both casters); then Warrior alone was retuned to 64 so a
    /// 16 CON Warrior heals every 4 turns instead of 3, and the user asked for that SAME rescale
    /// factor (64/46 ~= 1.391) applied to every other divisor too, preserving the original
    /// relative spacing between classes rather than leaving Warrior's change as a one-off.
    /// </summary>
    private static void ClassRegenDivisorsMatchDesignTable()
    {
        Check("Warrior HpRegenDivisor is 64 (a 16 CON Warrior heals every 4 turns)", CharacterClass.Warrior.HpRegenDivisor == 64);
        Check("Thief HpRegenDivisor is 89 (64 rescaled from the original 64, i.e. 64 * 64/46)", CharacterClass.Thief.HpRegenDivisor == 89);
        Check("Priest HpRegenDivisor is 89, same as Thief", CharacterClass.Priest.HpRegenDivisor == 89);
        Check("Mage HpRegenDivisor is 114 (82 rescaled by 64/46)", CharacterClass.Mage.HpRegenDivisor == 114);
        Check("Mage ManaRegenDivisor is 64 (46 rescaled by 64/46 -- mirrors Warrior's own HP divisor, as before)", CharacterClass.Mage.ManaRegenDivisor == 64);
        Check("Priest ManaRegenDivisor is 64, same as Mage's", CharacterClass.Priest.ManaRegenDivisor == 64);
    }

    private static Player MakeRegenTestPlayer(CharacterClass characterClass, Race race, int constitution, PrimaryAttribute? manaStatOverride = null, int manaStatValue = 0)
    {
        var stats = new CharacterStats();
        // Bypassing CharacterStats.Roll keeps ClassModifier at its default 0, so Adjusted
        // equals the BaseValue set here exactly (capped at 18, StatBlock.Adjusted's own
        // creation-value ceiling) -- no race/class stat bonus muddies the reference numbers.
        stats.Get(PrimaryAttribute.Constitution).BaseValue = constitution;
        if (manaStatOverride.HasValue)
        {
            stats.Get(manaStatOverride.Value).BaseValue = manaStatValue;
        }
        return new Player("RegenRateTester", characterClass, race, stats);
    }

    private static void HpRegenRateMatchesUserSpecifiedTargets()
    {
        var warrior = MakeRegenTestPlayer(CharacterClass.Warrior, Race.Human, constitution: 18);
        Check("An 18 CON Warrior's HP regen rate is exactly 18/64 per turn",
            Math.Abs(ResourceRegenerationCalculator.CalculateHpRegenRate(warrior, inCombat: false) - (18.0 / 64.0)) < 0.0001);

        var thief = MakeRegenTestPlayer(CharacterClass.Thief, Race.Human, constitution: 18);
        double thiefRate = ResourceRegenerationCalculator.CalculateHpRegenRate(thief, inCombat: false);
        Check("An 18 CON Thief's HP regen rate is exactly 18/89 per turn", Math.Abs(thiefRate - (18.0 / 89.0)) < 0.0001);

        var priest = MakeRegenTestPlayer(CharacterClass.Priest, Race.Human, constitution: 18);
        double priestRate = ResourceRegenerationCalculator.CalculateHpRegenRate(priest, inCombat: false);
        Check("An 18 CON Priest's HP regen rate matches the Thief's (same divisor)", Math.Abs(priestRate - thiefRate) < 0.0001);

        var mage = MakeRegenTestPlayer(CharacterClass.Mage, Race.Human, constitution: 18);
        Check("An 18 CON Mage's HP regen rate is exactly 18/114 per turn",
            Math.Abs(ResourceRegenerationCalculator.CalculateHpRegenRate(mage, inCombat: false) - (18.0 / 114.0)) < 0.0001);
    }

    private static void ManaRegenRateMatchesUserSpecifiedTargets()
    {
        var mage = MakeRegenTestPlayer(CharacterClass.Mage, Race.Human, constitution: 10, PrimaryAttribute.Knowledge, 18);
        Check("An 18 KNO Mage's mana regen rate is exactly 18/64 per turn",
            Math.Abs(ResourceRegenerationCalculator.CalculateManaRegenRate(mage, inCombat: false) - (18.0 / 64.0)) < 0.0001);

        var priest = MakeRegenTestPlayer(CharacterClass.Priest, Race.Human, constitution: 10, PrimaryAttribute.Wisdom, 18);
        Check("An 18 WIS Priest's mana regen rate is exactly 18/64 per turn",
            Math.Abs(ResourceRegenerationCalculator.CalculateManaRegenRate(priest, inCombat: false) - (18.0 / 64.0)) < 0.0001);

        var warrior = new Player("NoManaFloorWarrior", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(103)));
        var thief = new Player("NoManaFloorThief", CharacterClass.Thief, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Thief, new Random(104)));
        Check("Warrior and Thief mana regen rate is always exactly 0 -- no ManaStat at all, regardless of stat roll or combat state",
            ResourceRegenerationCalculator.CalculateManaRegenRate(warrior, inCombat: false) == 0
            && ResourceRegenerationCalculator.CalculateManaRegenRate(thief, inCombat: true) == 0);
    }

    /// <summary>Combat applies RegenerationConfig.CombatRegenMultiplier (1/10th) on top of the normal rate -- e.g. a Warrior's 0.5/turn drops to 0.05/turn while in combat.</summary>
    private static void CombatRegenRateIsOneTenthOfNormalRate()
    {
        var warrior = MakeRegenTestPlayer(CharacterClass.Warrior, Race.Human, constitution: 18);
        double normalRate = ResourceRegenerationCalculator.CalculateHpRegenRate(warrior, inCombat: false);
        double combatRate = ResourceRegenerationCalculator.CalculateHpRegenRate(warrior, inCombat: true);
        Check("In-combat HP regen rate is exactly 1/10th of the normal rate", Math.Abs(combatRate - (normalRate * 0.10)) < 0.0001);
    }

    /// <summary>
    /// Regen is now a sub-1 rate accumulated across turns (Player.HpRegenAccumulator) rather
    /// than an integer floored (with a minimum of 1) every single turn -- a Mage's slow rate
    /// should heal exactly +1 on the turn its accumulated fraction first reaches 1.0, not "+1
    /// every turn" (the old minimum-of-1 floor) and not "+0 forever" (the old floor(x*0.25) bug
    /// this whole regen system was rewritten to fix in the first place). This is the actual
    /// regression test for the original bug report: an 18 CON Dwarf Warrior "healing 1 HP per
    /// turn" regardless of Constitution. The expected turn is computed from the live rate
    /// (Ceiling(1/rate)) rather than hardcoded, so this stays correct across future divisor tuning.
    /// </summary>
    private static void FractionalRegenAccumulatesToWholePointsOnSchedule()
    {
        var mage = MakeRegenTestPlayer(CharacterClass.Mage, Race.Human, constitution: 18);
        mage.Health.SetCurrent(mage.Health.Max - 10);
        int before = mage.Health.Current;

        double rate = ResourceRegenerationCalculator.CalculateHpRegenRate(mage, inCombat: false);
        int expectedHealTurn = (int)Math.Ceiling(1.0 / rate);

        for (int turn = 1; turn < expectedHealTurn; turn++)
        {
            ResourceRegenerationCalculator.ApplyRegen(mage, inCombat: false);
            Check($"An 18 CON Mage has not yet healed a whole point after {turn} turn(s)", mage.Health.Current == before);
        }

        ResourceRegenerationCalculator.ApplyRegen(mage, inCombat: false);
        Check($"The same Mage heals exactly +1 HP on turn {expectedHealTurn}, right on schedule", mage.Health.Current == before + 1);
    }

    /// <summary>Constitution now visibly matters again -- the original bug report was that an 18 CON Dwarf Warrior and (implicitly) any lower-CON character regenerated identically at exactly 1 HP/turn, because floor(baseRegen/8) fed into a further *0.25 cut made Constitution's contribution disappear below the resolution of a single turn.</summary>
    private static void HigherConstitutionRegeneratesFasterOverTime()
    {
        var lowCon = MakeRegenTestPlayer(CharacterClass.Warrior, Race.Human, constitution: 6);
        var highCon = MakeRegenTestPlayer(CharacterClass.Warrior, Race.Human, constitution: 18);
        lowCon.Health.SetCurrent(lowCon.Health.Max - 20);
        highCon.Health.SetCurrent(highCon.Health.Max - 20);
        int lowBefore = lowCon.Health.Current;
        int highBefore = highCon.Health.Current;

        for (int turn = 0; turn < 10; turn++)
        {
            ResourceRegenerationCalculator.ApplyRegen(lowCon, inCombat: false);
            ResourceRegenerationCalculator.ApplyRegen(highCon, inCombat: false);
        }

        Check("Over 10 turns, an 18 CON Warrior heals strictly more than a 6 CON Warrior -- Constitution visibly matters again",
            (highCon.Health.Current - highBefore) > (lowCon.Health.Current - lowBefore));
    }

    private static void PlayerInCombatDetectsOwnAttacksAndAdjacentMonsterAttacks()
    {
        var rng = new Random(112);
        var level = BuildOpenLevel(5, 5);
        var player = new Player("CombatDetectTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng)) { X = 2, Y = 2 };

        Check("A player who hasn't attacked and has no adjacent monster is not considered in combat",
            !GameLoop.IsPlayerInCombat(level, player));

        level.RestoreTurnNumber(10);
        player.LastAttackTurn = 10;
        Check("A player who attacked this turn is in combat", GameLoop.IsPlayerInCombat(level, player));

        player.LastAttackTurn = 9;
        Check("A player who attacked last turn is still in combat", GameLoop.IsPlayerInCombat(level, player));

        player.LastAttackTurn = 8;
        Check("A player whose last attack was two turns ago is no longer in combat (absent anything else)",
            !GameLoop.IsPlayerInCombat(level, player));

        var adjacentMonster = Monster.CreateRandom(3, 2, 1, rng);
        adjacentMonster.LastAttackTurn = 9;
        level.Actors.Add(adjacentMonster);
        Check("A monster adjacent to the player that attacked last turn puts the player in combat too, even if the player didn't swing",
            GameLoop.IsPlayerInCombat(level, player));

        adjacentMonster.X = 4;
        adjacentMonster.Y = 4;
        Check("The same monster no longer counts once it's no longer adjacent, regardless of when it last attacked",
            !GameLoop.IsPlayerInCombat(level, player));
    }

    /// <summary>
    /// Regression test for a real bug report: natural regen stuck at the 1/10th combat rate for
    /// many turns on a brand new floor with no fighting at all. Level.TurnNumber is per-level (a
    /// freshly generated floor starts back at 0), but Player.LastAttackTurn persists across
    /// floors since the Player object is never recreated -- so a player who last attacked late
    /// in the previous level's turn count (e.g. turn 95) used to register as still "in combat"
    /// on the new floor until its own turn counter climbed back past that stale value (0 - 95 =
    /// -95, which the old "<= 1" check accepted). See GameLoop.AttackedRecently's own doc comment.
    /// </summary>
    private static void PlayerInCombatIgnoresAStaleLastAttackTurnFromAPreviousLevel()
    {
        var rng = new Random(113);
        var player = new Player("StaleCombatTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng)) { X = 2, Y = 2 };
        player.LastAttackTurn = 95; // set on a previous, deeper-turn-count level

        var freshLevel = BuildOpenLevel(5, 5); // TurnNumber starts at 0, like any newly generated floor
        Check("A player whose LastAttackTurn is from a previous level (a stale, larger turn number than this fresh level's TurnNumber) is NOT considered in combat",
            !GameLoop.IsPlayerInCombat(freshLevel, player));

        for (int i = 0; i < 5; i++)
        {
            freshLevel.AdvanceTurn();
        }
        Check("...and still isn't after a handful of turns pass on the new level, long before TurnNumber could ever reach the stale value",
            !GameLoop.IsPlayerInCombat(freshLevel, player));
    }

    // --- Trader inventory cap, multi-column layout, and "Get All" pickup --------------

    private static void TraderInventoryCapRejectsFurtherSellsWithVarietyMessage()
    {
        var rng = new Random(110);
        var trader = Trader.CreateRandom(1, 1, 1, rng);
        while (trader.Inventory.Items.Count < TraderConfig.TraderInventoryCap)
        {
            trader.Inventory.AddItem(Items.HealthPotion);
        }

        var player = new Player("CapTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        player.Inventory.AddItem(Items.Dagger.Clone());
        int traderCountBefore = trader.Inventory.Items.Count;

        var seenMessages = new HashSet<string>();
        for (int i = 0; i < 30; i++)
        {
            seenMessages.Add(TraderScreen.TrySell(player, trader, 0));
        }

        Check("A trader already at the inventory cap rejects a sell instead of accepting it -- the item never actually moves",
            player.Inventory.Items.Count == 1 && trader.Inventory.Items.Count == traderCountBefore);
        Check("The inventory-full rejection has more than one possible message for variety",
            seenMessages.Count > 1);
    }

    private static void TraderColumnLayoutRespectsSixteenPerColumnRule()
    {
        Check("Column count follows the \"at most 16 items per column\" rule",
            TraderScreen.ColumnCountFor(1) == 1
            && TraderScreen.ColumnCountFor(16) == 1
            && TraderScreen.ColumnCountFor(17) == 2
            && TraderScreen.ColumnCountFor(32) == 2
            && TraderScreen.ColumnCountFor(33) == 3
            && TraderScreen.ColumnCountFor(35) == 3);
    }

    /// <summary>Regression: MenuPrompt.Choose (used by TraderScreen's Inspect/Identify flows among many others) used to print one option per line with no column layout at all -- a trader with a large stock, or a full inventory to inspect, would scroll right past the console's own height.</summary>
    private static void MenuPromptColumnLayoutRespectsSixteenPerColumnRule()
    {
        Check("MenuPrompt.Choose's column count follows the same \"at most 16 per column\" rule as TraderScreen's own list",
            MenuPrompt.ColumnCountFor(1) == 1
            && MenuPrompt.ColumnCountFor(16) == 1
            && MenuPrompt.ColumnCountFor(17) == 2
            && MenuPrompt.ColumnCountFor(32) == 2
            && MenuPrompt.ColumnCountFor(33) == 3);
    }

    private static void PickUpAllPicksUpEveryItemOnTheTileInOneAction()
    {
        var rng = new Random(111);
        var player = new Player("GetAllTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;

        var itemA = Items.HealthPotion;
        var itemB = Items.ManaPotion;
        var itemC = Items.Dagger;
        level.AddItem(player.X, player.Y, itemA);
        level.AddItem(player.X, player.Y, itemB);
        level.AddItem(player.X, player.Y, itemC);
        var groundItems = level.GetItemsAt(player.X, player.Y);

        bool result = gameLoop.PickUpAll(level, groundItems);

        Check("\"Get All\" picks up every item on the tile in one action and clears the ground",
            result && level.GetItemsAt(player.X, player.Y).Count == 0
            && player.Inventory.Items.Contains(itemA) && player.Inventory.Items.Contains(itemB) && player.Inventory.Items.Contains(itemC));
    }

    // --- Damage/kill-credit attribution ------------------------------------------------

    private static void PlayerKillingAMonsterAwardsNormalXpAndGold()
    {
        var rng = new Random(140);
        var player = new Player("CreditTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;

        var monster = Monster.CreateRandom(player.X, player.Y, 1, rng);
        monster.Level = 10; // guarantees a non-zero gold-drop ceiling regardless of rng roll
        monster.LastDamageOwner = player; // simulates the kill coming from the player's own last hit
        monster.Health.TakeDamage(monster.Health.Max);
        level.Actors.Add(monster);

        gameLoop.AwardDeathRewards(level);

        Check("A monster the player is credited with killing shows an XP/gold reward in its death message",
            gameLoop.StatusMessages[^1].Contains("(+"));
    }

    private static void EnvironmentalKillAwardsNoXpOrGold()
    {
        var rng = new Random(141);
        var player = new Player("NoCreditTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;

        var monster = Monster.CreateRandom(player.X, player.Y, 1, rng);
        // LastDamageOwner left null -- as it would be after EnvironmentalFloorEffects/a trap kill.
        monster.Health.TakeDamage(monster.Health.Max);
        level.Actors.Add(monster);

        long goldBefore = player.Gold;
        long xpBefore = player.Experience.Current;
        int levelBefore = player.Level;

        gameLoop.AwardDeathRewards(level);

        Check("A monster killed by something other than the player (LastDamageOwner null) awards no XP or gold",
            player.Gold == goldBefore && player.Experience.Current == xpBefore && player.Level == levelBefore);
        Check("...and its death message carries no reward suffix",
            !gameLoop.StatusMessages[^1].Contains("(+"));
    }

    private static void OneMonsterKillingAnotherAwardsThePlayerNothing()
    {
        var rng = new Random(142);
        var player = new Player("MonsterVsMonsterTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;

        var attacker = Monster.CreateRandom(player.X, player.Y, 1, rng);
        var victim = Monster.CreateRandom(player.X, player.Y, 1, rng);
        victim.LastDamageOwner = attacker; // one monster killed another (friendly-fire projectile, etc.)
        victim.Health.TakeDamage(victim.Health.Max);
        level.Actors.Add(victim);

        long goldBefore = player.Gold;
        long xpBefore = player.Experience.Current;

        gameLoop.AwardDeathRewards(level);

        Check("A monster killed by another monster (LastDamageOwner is a Monster, not the Player) awards the player nothing",
            player.Gold == goldBefore && player.Experience.Current == xpBefore
            && !gameLoop.StatusMessages[^1].Contains("(+"));
    }

    private static void PlayerAppliedPoisonKillCreditsThePlayerEvenAfterTheyStoppedAttacking()
    {
        var rng = new Random(143);
        var player = new Player("DotCreditTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;

        var monster = Monster.CreateRandom(player.X, player.Y, 1, rng);
        monster.Health.SetCurrent(1);
        // A poison DoT the player applied, ticking on a later turn than the player's actual attack.
        monster.ActiveEffects.Add(new ActiveEffect("Poisoned", level.TurnNumber + 5)
        {
            TickDamage = 50,
            TickDamageType = DamageType.Poison,
            DamageSourceDescription = "your poisoned blade",
            Owner = player
        });
        level.Actors.Add(monster);

        EffectProcessor.Tick(level);

        Check("A DoT tick propagates its Owner to LastDamageOwner, so a delayed poison kill still credits the player",
            !monster.IsAlive && monster.LastDamageOwner == player);

        gameLoop.AwardDeathRewards(level);
        Check("...and AwardDeathRewards honors that credit with a normal reward message",
            gameLoop.StatusMessages[^1].Contains("(+"));
    }

    private static void EnvironmentalDamageNeverLeavesAStalePlayerOwnedCreditBehind()
    {
        var rng = new Random(144);
        var player = new Player("StaleCreditTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng)) { X = 1, Y = 1 };
        var level = new Level(1, 3, 3);
        for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) level.Tiles[x, y] = Tile.CreateFloor();
        level.Tiles[1, 1].FloorType = FloorType.Fire;

        var earlierAttacker = Monster.CreateRandom(0, 0, 1, rng);
        player.LastDamageOwner = earlierAttacker; // a stale non-owner credit from an earlier, unrelated hit

        EnvironmentalFloorEffects.Apply(level, player);

        Check("Standing on Fire explicitly clears a stale LastDamageOwner rather than merely leaving it unset",
            player.LastDamageOwner == null);
    }

    private static void LootStillDropsRegardlessOfWhoGetsKillCredit()
    {
        var rng = new Random(145);
        var player = new Player("LootAlwaysDropsTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;

        var monster = Monster.CreateRandom(player.X, player.Y, 1, rng);
        var weapon = Items.Dagger.Clone();
        var slot = monster.Equipment.FindAutoEquipSlot(weapon);
        monster.Equipment.EquipInSlot(slot!.Value, weapon);
        // LastDamageOwner left null -- an environmental/trap kill, no player credit.
        monster.Health.TakeDamage(monster.Health.Max);
        level.Actors.Add(monster);

        gameLoop.AwardDeathRewards(level);

        var corpse = level.GetCorpsesAt(monster.X, monster.Y).FirstOrDefault();
        Check("Loot generation is unconditional -- an uncredited kill still drops what the monster was carrying (Corpse System: now inside its corpse rather than loose on the ground)",
            corpse != null && corpse.Container.Contents.Items.Any(i => i.Name == weapon.Name));
    }

    // --- Item Flammable/Fireproof/LavaProof, item destruction on landing ---------------

    private static Level BuildSingleTileLevel(FloorType floorType)
    {
        var level = new Level(1, 3, 3);
        for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) level.Tiles[x, y] = Tile.CreateFloor();
        level.Tiles[1, 1].FloorType = floorType;
        return level;
    }

    private static void ScrollsSpellbooksAndAmmunitionDefaultToFlammable()
    {
        Check("A Scroll defaults to Flammable with no per-item catalog edit needed", Items.ScrollOfIdentify.Flammable);
        Check("Ammunition (Arrow) defaults to Flammable", Items.Arrow.Flammable);
    }

    private static void WeaponsArmorAndPotionsDefaultToNonFlammable()
    {
        Check("A weapon defaults to non-Flammable", !Items.Dagger.Flammable);
        Check("A potion defaults to non-Flammable", !Items.HealthPotion.Flammable);
    }

    private static void FlammableItemIsDestroyedLandingOnFire()
    {
        var level = BuildSingleTileLevel(FloorType.Fire);
        var item = Items.ScrollOfIdentify.Clone();

        string message = ItemDestructionRules.LandOnFloor(level, 1, 1, item);

        Check("A Flammable item landing on Fire is destroyed with a message instead of being added to the ground",
            message != null && level.GetItemsAt(1, 1).Count == 0);
    }

    private static void FlammableItemIsDestroyedLandingOnLava()
    {
        var level = BuildSingleTileLevel(FloorType.Lava);
        var item = Items.Arrow.Clone();

        string message = ItemDestructionRules.LandOnFloor(level, 1, 1, item);

        Check("A Flammable item landing on Lava is destroyed with a message instead of being added to the ground",
            message != null && level.GetItemsAt(1, 1).Count == 0);
    }

    private static void NonFlammableItemNeverDestroyedByFireOrLava()
    {
        var fireLevel = BuildSingleTileLevel(FloorType.Fire);
        var lavaLevel = BuildSingleTileLevel(FloorType.Lava);
        var weaponOnFire = Items.Dagger.Clone();
        var weaponOnLava = Items.Dagger.Clone();

        string fireMessage = ItemDestructionRules.LandOnFloor(fireLevel, 1, 1, weaponOnFire);
        string lavaMessage = ItemDestructionRules.LandOnFloor(lavaLevel, 1, 1, weaponOnLava);

        Check("A non-Flammable item survives landing on Fire",
            fireMessage == null && fireLevel.GetItemsAt(1, 1).Contains(weaponOnFire));
        Check("A non-Flammable item survives landing on Lava",
            lavaMessage == null && lavaLevel.GetItemsAt(1, 1).Contains(weaponOnLava));
    }

    private static void FireproofItemSurvivesFireButNotLava()
    {
        var fireproofScroll = new Item("Fireproof Tome", '?', "test", ItemType.Spellbook, fireproof: true);

        var fireLevel = BuildSingleTileLevel(FloorType.Fire);
        string fireMessage = ItemDestructionRules.LandOnFloor(fireLevel, 1, 1, fireproofScroll);
        Check("A Flammable+Fireproof item survives landing on Fire",
            fireMessage == null && fireLevel.GetItemsAt(1, 1).Contains(fireproofScroll));

        var lavaLevel = BuildSingleTileLevel(FloorType.Lava);
        string lavaMessage = ItemDestructionRules.LandOnFloor(lavaLevel, 1, 1, fireproofScroll);
        Check("...but Fireproof alone does not protect it from Lava",
            lavaMessage != null && lavaLevel.GetItemsAt(1, 1).Count == 0);
    }

    private static void LavaProofItemSurvivesLavaButNotPlainFire()
    {
        var lavaProofScroll = new Item("Obsidian-Bound Tome", '?', "test", ItemType.Spellbook, lavaProof: true);

        var lavaLevel = BuildSingleTileLevel(FloorType.Lava);
        string lavaMessage = ItemDestructionRules.LandOnFloor(lavaLevel, 1, 1, lavaProofScroll);
        Check("A Flammable+LavaProof item survives landing on Lava",
            lavaMessage == null && lavaLevel.GetItemsAt(1, 1).Contains(lavaProofScroll));

        var fireLevel = BuildSingleTileLevel(FloorType.Fire);
        string fireMessage = ItemDestructionRules.LandOnFloor(fireLevel, 1, 1, lavaProofScroll);
        Check("...but LavaProof alone does not protect it from plain Fire",
            fireMessage != null && fireLevel.GetItemsAt(1, 1).Count == 0);
    }

    // --- MessageLog, explicit stairs commands ------------------------------------------

    private static void MessageLogCapsHistoryAtTwentyEntriesDroppingOldestFirst()
    {
        var log = new MessageLog();
        for (int i = 0; i < 25; i++)
        {
            log.Add($"Message {i}");
        }

        Check("MessageLog caps its retained history at 20 entries", log.History.Count == 20);
        Check("...dropping the oldest entries first, so the earliest surviving one is #5",
            log.History[0].Text == "Message 5" && log.History[^1].Text == "Message 24");
    }

    private static void MessageLogGetRecentReturnsOnlyTheMostRecentEntriesInOrder()
    {
        var log = new MessageLog();
        for (int i = 0; i < 5; i++)
        {
            log.Add($"Message {i}");
        }

        var recent = log.GetRecent(3);

        Check("GetRecent returns exactly the requested count, oldest-first, from the tail of history",
            recent.Count == 3 && recent[0].Text == "Message 2" && recent[1].Text == "Message 3" && recent[2].Text == "Message 4");
    }

    private static void DescendingWhileStandingOnStairsDownAdvancesAFloorAndConsumesATurn()
    {
        var rng = new Random(150);
        var player = new Player("StairsDownTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        player.X = level.StairsDownPosition.X;
        player.Y = level.StairsDownPosition.Y;

        bool turnConsumed = gameLoop.HandleUseStairs(level, descending: true);

        Check("Descending while standing on the down staircase moves to floor 2 and consumes a turn",
            turnConsumed && gameLoop.CurrentLevel.FloorIndex == 2);
    }

    private static void AscendingWhileStandingOnStairsUpReturnsAFloorAndConsumesATurn()
    {
        var rng = new Random(151);
        var player = new Player("StairsUpTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var firstFloorLevel = gameLoop.CurrentLevel;
        player.X = firstFloorLevel.StairsDownPosition.X;
        player.Y = firstFloorLevel.StairsDownPosition.Y;
        gameLoop.HandleUseStairs(firstFloorLevel, descending: true); // now on floor 2, standing on its stairs-up
        var secondFloorLevel = gameLoop.CurrentLevel;

        bool turnConsumed = gameLoop.HandleUseStairs(secondFloorLevel, descending: false);

        Check("Ascending while standing on the up staircase returns to floor 1 and consumes a turn",
            turnConsumed && gameLoop.CurrentLevel.FloorIndex == 1);
    }

    private static void StandingOnTheWrongStairwayShowsAMessageAndConsumesNoTurn()
    {
        var rng = new Random(152);
        var player = new Player("WrongStairwayTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        player.X = level.StairsDownPosition.X;
        player.Y = level.StairsDownPosition.Y;

        bool turnConsumed = gameLoop.HandleUseStairs(level, descending: false);

        Check("Trying to ascend while standing on the down staircase fails, consumes no turn, and says so",
            !turnConsumed && gameLoop.StatusMessages[^1].Contains("not up"));
    }

    private static void UsingStairsAwayFromAnyStairwayShowsAMessageAndConsumesNoTurn()
    {
        var rng = new Random(153);
        var player = new Player("NoStairwayTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        player.X = -1; // guaranteed not to match either stairway regardless of dungeon layout
        player.Y = -1;

        bool turnConsumed = gameLoop.HandleUseStairs(level, descending: true);

        Check("Using stairs away from any stairway fails, consumes no turn, and says there are none here",
            !turnConsumed && gameLoop.StatusMessages[^1].Contains("no stairs here"));
    }

    private static void AscendingFromTheTopmostFloorFailsAndConsumesNoTurn()
    {
        var rng = new Random(154);
        var player = new Player("TopmostFloorTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player); // starts on floor 1, standing exactly on its stairs-up
        var level = gameLoop.CurrentLevel;

        bool turnConsumed = gameLoop.HandleUseStairs(level, descending: false);

        Check("Ascending from floor 1 fails, consumes no turn, and explains why",
            !turnConsumed && gameLoop.StatusMessages[^1].Contains("topmost"));
    }

    // --- Atmospheric status-expiration messages ----------------------------------------

    private static void KnownAilmentExpirationUsesAtmosphericWordingNotGenericFades()
    {
        var rng = new Random(160);
        var level = new Level(1, 5, 5);
        var player = new Player("AtmosphericTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        player.ActiveEffects.Add(new ActiveEffect("Burning", level.TurnNumber));
        level.Actors.Add(player);

        var messages = EffectProcessor.Tick(level);

        Check("A known ailment (Burning) expiring uses atmospheric wording instead of the generic 'fades'",
            messages.Count == 1 && messages[0].Contains("burns die down") && !messages[0].Contains("fades"));
    }

    private static void UnknownEffectExpirationFallsBackToGenericFadesWording()
    {
        var rng = new Random(161);
        var level = new Level(1, 5, 5);
        var player = new Player("GenericFadeTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        player.ActiveEffects.Add(new ActiveEffect("Bless", level.TurnNumber) { ModifiedStat = Stat.Accuracy, StatAmount = 2 });
        level.Actors.Add(player);

        var messages = EffectProcessor.Tick(level);

        Check("An unrecognized spell/buff name expiring falls back to the original 'fades' phrasing",
            messages.Count == 1 && messages[0] == "Your Bless fades.");
    }

    private static void VisibleMonsterExpirationShowsAMessage()
    {
        var rng = new Random(162);
        var level = new Level(1, 5, 5);
        var monster = Monster.CreateRandom(2, 2, 1, rng);
        level.Tiles[2, 2].IsVisible = true;
        monster.ActiveEffects.Add(new ActiveEffect("Poisoned", level.TurnNumber));
        level.Actors.Add(monster);

        var messages = EffectProcessor.Tick(level);

        Check("A visible monster's expiring effect shows an atmospheric message naming it",
            messages.Count == 1 && messages[0].Contains("poison") && messages[0].Contains("'s"));
    }

    private static void OffScreenMonsterExpirationShowsNoMessage()
    {
        var rng = new Random(163);
        var level = new Level(1, 5, 5);
        var monster = Monster.CreateRandom(2, 2, 1, rng);
        // level.Tiles[2, 2].IsVisible left false -- off-screen, like an unexplored/unseen tile.
        monster.ActiveEffects.Add(new ActiveEffect("Poisoned", level.TurnNumber));
        level.Actors.Add(monster);

        var messages = EffectProcessor.Tick(level);

        Check("An off-screen monster's expiring effect produces no message at all",
            messages.Count == 0);
    }

    private static void NoExpirationMessageWhenTheExpiringDamageJustKilledTheActor()
    {
        var rng = new Random(164);
        var level = new Level(1, 5, 5);
        var monster = Monster.CreateRandom(2, 2, 1, rng);
        level.Tiles[2, 2].IsVisible = true;
        monster.Health.SetCurrent(1);
        monster.ActiveEffects.Add(new ActiveEffect("Poisoned", level.TurnNumber) { TickDamage = 999, TickDamageType = DamageType.Poison });
        level.Actors.Add(monster);

        var messages = EffectProcessor.Tick(level);

        Check("An effect whose own final tick kills the actor produces no expiration message",
            !monster.IsAlive && messages.Count == 0);
    }

    // --- Title screen / story screen ----------------------------------------------------

    private static void CenterPadsTextRoughlyEquallyOnBothSides()
    {
        // Only left-padded (trailing spaces are invisible on a terminal, so there's nothing to
        // add on the right) -- (width - text.Length) / 2 leading spaces puts the visible text
        // roughly in the middle of the line.
        string centered = MainMenuScreen.Center("HI", 10);

        Check("Center left-pads a short string so it sits roughly in the middle of the given width",
            centered == new string(' ', 4) + "HI");
    }

    private static void CenterReturnsTextUnchangedWhenItAlreadyFillsOrExceedsTheWidth()
    {
        string exact = MainMenuScreen.Center("EXACTLYTEN", 10);
        string tooLong = StoryScreen.Center("This text is longer than the width", 10);

        Check("Center leaves text unchanged when it already fills the width", exact == "EXACTLYTEN");
        Check("...or already exceeds it", tooLong == "This text is longer than the width");
    }

    private static void MainMenuTitleBlockContainsTheGameTitleAndSubtitle()
    {
        string block = MainMenuScreen.BuildTitleBlock();

        Check("The main menu's title block shows the game's title", block.Contains("BENEATH FORGOTTEN STONE"));
        Check("...and its subtitle", block.Contains("The deep remembers."));
    }

    private static void StoryScreenBuildBlocksPreservesParagraphsAsAtomicUnitsAndWrapsToWidth()
    {
        // At width 20, "First paragraph here." (22 chars) and "Second paragraph
        // here." (23 chars) each need to break after "paragraph" -- chosen deliberately so this
        // test also proves wrapping actually happens, not just that the raw text passes through.
        string body = "First paragraph here.\n\nSecond paragraph here.";
        var blocks = StoryScreen.BuildBlocks("A TITLE", body, width: 20);

        Check("BuildBlocks' first block is the centered title, alone", blocks[0].Count == 1 && blocks[0][0].Contains("A TITLE"));
        Check("Each paragraph becomes its own block, wrapped to the given width",
            blocks[1].SequenceEqual(new[] { "First paragraph", "here." })
            && blocks[2].SequenceEqual(new[] { "Second paragraph", "here." }));
        Check("Every wrapped line respects the given width", blocks.SelectMany(b => b).All(l => l.Length <= 20));
    }

    private static void StoryScreenPaginateNeverSplitsABlockAcrossAPageBreak()
    {
        // Regression check for a real bug report: with flat-line pagination, a page break could
        // fall in the MIDDLE of a wrapped paragraph, cutting a sentence off mid-word with no
        // continuation indicator. Sized so the second block (3 lines) doesn't fit in what's left
        // of a 5-line page after the first block (4 lines) -- the old flat-line pager would have
        // split it 1 line on page 1, 2 lines on page 2; block-based pagination must instead move
        // the whole block to page 2.
        var blocks = new List<List<string>>
        {
            new() { "Block A line 1", "Block A line 2", "Block A line 3", "Block A line 4" },
            new() { "Block B line 1", "Block B line 2", "Block B line 3" }
        };

        var pages = StoryScreen.Paginate(blocks, pageSize: 5);

        Check("A block that doesn't fit in the remaining space moves entirely to the next page instead of being cut mid-paragraph",
            pages.Count == 2
            && pages[0].SequenceEqual(new[] { "Block A line 1", "Block A line 2", "Block A line 3", "Block A line 4" })
            && pages[1].SequenceEqual(new[] { "Block B line 1", "Block B line 2", "Block B line 3" }));
    }

    private static void StoryScreenPaginateSeparatesBlocksOnTheSamePageWithExactlyOneBlankLine()
    {
        var blocks = new List<List<string>> { new() { "First" }, new() { "Second" } };

        var pages = StoryScreen.Paginate(blocks, pageSize: 10);

        Check("Two blocks that both fit on one page are joined by exactly one blank line",
            pages.Count == 1 && pages[0].SequenceEqual(new[] { "First", "", "Second" }));
    }

    private static void StoryScreenPaginateAlwaysReturnsAtLeastOnePage()
    {
        var pages = StoryScreen.Paginate(new List<List<string>>(), pageSize: 10);

        Check("Paginate returns exactly one (empty) page for an empty block list, never zero pages",
            pages.Count == 1 && pages[0].Count == 0);
    }

    /// <summary>
    /// Regression check for a real bug report: the opening story's first page scrolled the title
    /// off the top of the visible window, and a later fix for that (fixed 60x24 dimensions
    /// instead of a live, ConPTY-unreliable Console.WindowHeight query) surfaced a second bug --
    /// flat-line pagination cutting a paragraph in half across a page break. This reproduces the
    /// real opening story against the real fixed dimensions and confirms every page fits AND no
    /// paragraph got split.
    /// </summary>
    private static void OpeningStoryAtFixedConsoleDimensionsFitsAndNeverSplitsAParagraph()
    {
        const int width = 56; // ConsoleWidth(60) - HorizontalMargin(2) * 2
        const int pageSize = 21; // ConsoleHeight(24) - FooterLines(2) - 1

        var blocks = StoryScreen.BuildBlocks("THE OLD KINGDOM", StoryScreen.OpeningStoryText, width);
        var pages = StoryScreen.Paginate(blocks, pageSize);

        Check("The opening story is long enough to actually exercise pagination (more than one page)",
            pages.Count > 1);

        Check("Every page (content + blank + footer) fits within the fixed 24-row console, on every page including the first",
            pages.All(page => page.Count + 2 <= 24)); // +2 for the blank line + footer every page adds

        Check("The title survives as the very first line of the very first page (never pushed off by overflow)",
            pages[0][0].Contains("THE OLD KINGDOM"));

        // Every non-title block's full text must appear, unbroken, within a single page -- i.e.
        // for each paragraph, some one page's joined text contains that paragraph's own first
        // wrapped line immediately followed (across a join with a space) by the rest of its lines.
        bool noBlockWasSplitAcrossPages = blocks.Skip(1).All(block =>
            pages.Any(page => IndexOfSubsequence(page, block) >= 0));
        Check("No paragraph is split across two pages -- each one lands whole on a single page",
            noBlockWasSplitAcrossPages);
    }

    /// <summary>Index of the first occurrence of `needle` as a contiguous run within `haystack`, or -1 if it never appears whole.</summary>
    private static int IndexOfSubsequence(List<string> haystack, List<string> needle)
    {
        for (int i = 0; i <= haystack.Count - needle.Count; i++)
        {
            bool matches = true;
            for (int j = 0; j < needle.Count; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    matches = false;
                    break;
                }
            }
            if (matches)
            {
                return i;
            }
        }
        return -1;
    }

    private static void OpeningStoryTextContainsEveryKeyBeat()
    {
        string story = StoryScreen.OpeningStoryText;

        Check("The opening story mentions the dwarven halls", story.Contains("dwarven halls"));
        Check("...the Crown's soldiers", story.Contains("The Crown sent soldiers"));
        Check("...and the Old Kingdom's gates", story.Contains("Old Kingdom"));
    }

    // --- Dark rooms, illumination, physical/magical light sources -----------------------

    private static Level BuildDarkOpenLevel(int width, int height)
    {
        var level = new Level(1, width, height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                level.Tiles[x, y] = Tile.CreateFloor();
                level.Tiles[x, y].IsDarkRoom = true;
            }
        }
        return level;
    }

    private static void DarkRoomTileHiddenWithoutIllumination()
    {
        var level = BuildDarkOpenLevel(5, 5);

        FieldOfView.Compute(level, 2, 2, radius: 8);

        Check("A dark-room tile within line of sight but not illuminated stays hidden and unexplored",
            !level.Tiles[3, 3].IsVisible && !level.Tiles[3, 3].IsExplored);
    }

    private static void DarkRoomTileVisibleOnceIlluminated()
    {
        var level = BuildDarkOpenLevel(5, 5);
        level.Tiles[3, 3].IsIlluminated = true;

        FieldOfView.Compute(level, 2, 2, radius: 8);

        Check("A dark-room tile within line of sight AND illuminated becomes visible and explored",
            level.Tiles[3, 3].IsVisible && level.Tiles[3, 3].IsExplored);
    }

    private static void NonDarkRoomTileNeedsOnlyLineOfSight()
    {
        var level = BuildOpenLevel(5, 5); // ordinary tiles -- IsDarkRoom false, unaffected by darkness

        FieldOfView.Compute(level, 2, 2, radius: 8);

        Check("An ordinary (non-dark) room tile only needs line of sight -- behaves exactly as before darkness existed",
            level.Tiles[3, 3].IsVisible && level.Tiles[3, 3].IsExplored);
    }

    /// <summary>
    /// Regression check for a real bug report: a door on a dark room's own boundary (never
    /// marked IsDarkRoom itself -- see PaintDarkRoomTiles) was visible via plain unobstructed
    /// geometry from ANYWHERE inside the room, even standing on a pitch-black, unlit tile with no
    /// light source at all -- letting a player "see" every exit the instant they walked in,
    /// without ever having discovered any of them. Darkness should blind the OBSERVER, not just
    /// hide dark tiles: a player standing on a dark, unilluminated tile can perceive only what's
    /// actually illuminated right now, or what they've already explored from an earlier, sighted
    /// encounter -- never something newly discoverable by geometry alone while blind.
    /// </summary>
    private static void BlindObserverCannotDiscoverAnUnseenNonDarkTileAcrossADarkRoom()
    {
        var level = BuildDarkOpenLevel(5, 5);
        level.Tiles[3, 3].IsDarkRoom = false; // an ordinary tile (e.g. a door) sitting on the room's boundary

        FieldOfView.Compute(level, 2, 2, radius: 8); // origin (2,2) is dark and unlit -- the observer is blind

        Check("A blind observer cannot newly discover an ordinary, never-explored tile just because geometry reaches it",
            !level.Tiles[3, 3].IsVisible && !level.Tiles[3, 3].IsExplored);
    }

    private static void BlindObserverStillSeesAPreviouslyExploredTileAsRemembered()
    {
        var level = BuildDarkOpenLevel(5, 5);
        level.Tiles[3, 3].IsDarkRoom = false;
        level.Tiles[3, 3].IsExplored = true; // discovered earlier, e.g. from its own lit approach

        FieldOfView.Compute(level, 2, 2, radius: 8); // blind observer

        Check("A blind observer still recognizes a tile they've already explored before -- remembered (IsExplored stays true), but not freshly IsVisible",
            !level.Tiles[3, 3].IsVisible && level.Tiles[3, 3].IsExplored);
    }

    private static void BlindObserverSeesAnyCurrentlyIlluminatedTileRegardlessOfPriorExploration()
    {
        var level = BuildDarkOpenLevel(5, 5);
        level.Tiles[3, 3].IsDarkRoom = false;
        level.Tiles[3, 3].IsIlluminated = true; // e.g. a torch someone is carrying reaches it right now

        FieldOfView.Compute(level, 2, 2, radius: 8); // blind observer

        Check("A blind observer still sees whatever a current light source actually illuminates, never-explored or not",
            level.Tiles[3, 3].IsVisible && level.Tiles[3, 3].IsExplored);
    }

    private static void IlluminationRespectsWalls()
    {
        var level = new Level(1, 7, 3);
        for (int x = 0; x < 7; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                level.Tiles[x, y] = Tile.CreateFloor();
            }
        }
        level.Tiles[3, 1] = Tile.CreateWall(); // sits directly between the source and the far tile

        var visible = FieldOfView.ComputeVisibleCells(level, 0, 1, radius: 8);

        Check("A light source's own reach is blocked by a wall, even within its configured radius",
            !visible.Contains((6, 1)));
        Check("...but tiles on the near side of that same wall are still reached",
            visible.Contains((2, 1)));
    }

    private static void CollectActiveLightSourcesFindsLitItemsAndLightEffects()
    {
        var rng = new Random(200);
        var level = new Level(1, 5, 5);
        var player = new Player("LightSourceTester", CharacterClass.Mage, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Mage, rng)) { X = 1, Y = 1 };
        var torch = Items.Torch.Clone();
        torch.IsLit = true;
        player.Inventory.AddItem(torch);
        player.ActiveEffects.Add(new ActiveEffect("Arcane Orb", 999) { LightRadius = 6 });
        level.Actors.Add(player);

        var sources = LightingSystem.CollectActiveLightSources(level);

        Check("A lit item contributes a light source at its carrier's position with its own radius",
            sources.Any(s => s.X == 1 && s.Y == 1 && s.Radius == Items.Torch.LightRadius));
        Check("A LightRadius-bearing ActiveEffect also contributes a light source at its owner's position",
            sources.Any(s => s.X == 1 && s.Y == 1 && s.Radius == 6));
    }

    private static void UnlitItemIsNotACollectedLightSource()
    {
        var rng = new Random(201);
        var level = new Level(1, 5, 5);
        var player = new Player("UnlitTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        player.Inventory.AddItem(Items.Torch.Clone()); // IsLit false by default
        level.Actors.Add(player);

        var sources = LightingSystem.CollectActiveLightSources(level);

        Check("Merely carrying an unlit light source contributes no illumination", sources.Count == 0);
    }

    private static void CandleTorchLanternMatchDesignedBalanceValues()
    {
        Check("Candle matches its designed radius/duration/extinguish-chance/water/consumed values",
            Items.Candle.EmitsLight && Items.Candle.LightRadius == LightingConfig.CandleLightRadius
            && Items.Candle.MaxLightDuration == LightingConfig.CandleMaxDuration
            && Items.Candle.ExtinguishChancePerTurn == LightingConfig.CandleExtinguishChancePerTurn
            && Items.Candle.ExtinguishedByWater && Items.Candle.DestroyedWhenLightExhausted);

        Check("Torch matches its designed values",
            Items.Torch.EmitsLight && Items.Torch.LightRadius == LightingConfig.TorchLightRadius
            && Items.Torch.MaxLightDuration == LightingConfig.TorchMaxDuration
            && Items.Torch.ExtinguishChancePerTurn == LightingConfig.TorchExtinguishChancePerTurn
            && Items.Torch.ExtinguishedByWater && Items.Torch.DestroyedWhenLightExhausted);

        Check("Lantern matches its designed values, and is preserved (not destroyed) when exhausted",
            Items.Lantern.EmitsLight && Items.Lantern.LightRadius == LightingConfig.LanternLightRadius
            && Items.Lantern.MaxLightDuration == LightingConfig.LanternMaxDuration
            && Items.Lantern.ExtinguishChancePerTurn == LightingConfig.LanternExtinguishChancePerTurn
            && Items.Lantern.ExtinguishedByWater && !Items.Lantern.DestroyedWhenLightExhausted);

        Check("Every light source starts unlit with a full RemainingLightDuration",
            !Items.Candle.IsLit && Items.Candle.RemainingLightDuration == LightingConfig.CandleMaxDuration);
    }

    private static void LightingSystemProcessTurnDecrementsDurationAndDestroysExhaustedConsumables()
    {
        var rng = new Random(202);
        var player = new Player("BurnDownTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var torch = Items.Torch.Clone();
        torch.IsLit = true;
        torch.RemainingLightDuration = 1;
        player.Inventory.AddItem(torch);

        var messages = LightingSystem.ProcessTurn(player, rng);

        Check("A physical light's duration hitting 0 extinguishes it and produces a burn-out message",
            !torch.IsLit && torch.RemainingLightDuration == 0 && messages.Count == 1);
        Check("A DestroyedWhenLightExhausted item (Torch) is removed from inventory once exhausted",
            !player.Inventory.Items.Contains(torch));
    }

    private static void LightingSystemProcessTurnPreservesAnExhaustedLantern()
    {
        var rng = new Random(203);
        var player = new Player("LanternPreserveTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var lantern = Items.Lantern.Clone();
        lantern.IsLit = true;
        lantern.RemainingLightDuration = 1;
        player.Inventory.AddItem(lantern);

        LightingSystem.ProcessTurn(player, rng);

        Check("An exhausted Lantern goes dark but is preserved in inventory, not destroyed",
            !lantern.IsLit && player.Inventory.Items.Contains(lantern));
    }

    private static void RandomExtinguishNeverConsumesRemainingDuration()
    {
        var rng = new Random(5);
        var player = new Player("RandomExtinguishTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        // A bespoke 100%-chance light (the real catalog items' chances are far too low to
        // reliably trigger in a single deterministic call) -- everything else about it matches
        // a normal physical light source.
        var guaranteedFlicker = new Item("Test Flicker Torch", '!', "test", ItemType.LightSource,
            emitsLight: true, lightRadius: 4, maxLightDuration: 100, extinguishChancePerTurn: 1.0, extinguishedByWater: true)
        {
            IsLit = true
        };
        player.Inventory.AddItem(guaranteedFlicker);

        var messages = LightingSystem.ProcessTurn(player, rng);

        Check("A random extinguish (forced to 100% chance) turns off the light but only burns the one normal turn of duration -- the snuff itself costs nothing extra",
            !guaranteedFlicker.IsLit && guaranteedFlicker.RemainingLightDuration == 99 && messages.Count == 1);
    }

    private static void ExtinguishForWaterPutsOutLitWaterVulnerableItemsOnlyOnce()
    {
        var rng = new Random(204);
        var player = new Player("WaterTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var torch = Items.Torch.Clone();
        torch.IsLit = true;
        torch.RemainingLightDuration = 77;
        player.Inventory.AddItem(torch);

        var firstMessages = LightingSystem.ExtinguishForWater(player);
        Check("Entering water extinguishes a lit, water-vulnerable light without touching its remaining duration",
            !torch.IsLit && torch.RemainingLightDuration == 77 && firstMessages.Count == 1);

        var secondMessages = LightingSystem.ExtinguishForWater(player);
        Check("A second call (standing in water another turn) finds nothing left to extinguish -- no repeated message",
            secondMessages.Count == 0);
    }

    private static void LightingAnItemViaInventoryTogglesIsLit()
    {
        var rng = new Random(205);
        var player = new Player("LightToggleTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        var torch = Items.Torch.Clone();
        player.Inventory.AddItem(torch);

        string message = InventoryScreen.ApplyItem(player, torch, level);

        Check("Applying an unlit light source from the inventory screen lights it",
            torch.IsLit && message.Contains("light"));
    }

    private static void RelightingAnExtinguishedItemResumesFromRemainingDurationNotFull()
    {
        var rng = new Random(206);
        var player = new Player("RelightTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        var torch = Items.Torch.Clone();
        torch.IsLit = true;
        torch.RemainingLightDuration = 40;
        player.Inventory.AddItem(torch);

        string extinguishMessage = InventoryScreen.ApplyItem(player, torch, level);
        Check("Extinguishing preserves remaining duration and reports it",
            !torch.IsLit && torch.RemainingLightDuration == 40 && extinguishMessage.Contains("40"));

        string relightMessage = InventoryScreen.ApplyItem(player, torch, level);
        Check("Relighting the same item resumes from its preserved remaining duration instead of resetting to full",
            torch.IsLit && torch.RemainingLightDuration == 40);
    }

    private static void LightingAnAlreadyExhaustedItemFails()
    {
        var rng = new Random(207);
        var player = new Player("ExhaustedTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        var lantern = Items.Lantern.Clone();
        lantern.RemainingLightDuration = 0;
        player.Inventory.AddItem(lantern);

        string message = InventoryScreen.ApplyItem(player, lantern, level);

        Check("Attempting to light an already-exhausted item fails without setting IsLit",
            !lantern.IsLit && message.Contains("nothing left to burn"));
    }

    private static void PlayerInDarknessReflectsTileDarkAndIlluminationState()
    {
        var rng = new Random(208);
        var player = new Player("DarknessFlagTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng)) { X = 1, Y = 1 };
        var level = new Level(1, 3, 3);
        for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) level.Tiles[x, y] = Tile.CreateFloor();

        Check("A non-dark tile is never 'in darkness' regardless of illumination",
            !GameLoop.IsPlayerInDarkness(level, player));

        level.Tiles[1, 1].IsDarkRoom = true;
        Check("A dark, unilluminated tile IS 'in darkness'", GameLoop.IsPlayerInDarkness(level, player));

        level.Tiles[1, 1].IsIlluminated = true;
        Check("The same dark tile, once illuminated, is no longer 'in darkness'",
            !GameLoop.IsPlayerInDarkness(level, player));
    }

    private static void AttackFromDarknessHidesAttackerIdentityInBothHitAndMissMessages()
    {
        var rng = new Random(209);
        var player = new Player("HiddenAttackTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var monster = Monster.CreateRandom(0, 0, 1, rng);

        var hitResult = new AttackResult { Attacker = monster, Defender = player, Hit = true, Damage = 4, Severity = HitSeverity.Minor, AttackType = AttackType.Bite };
        string hitMessage = CombatMessages.Format(hitResult, attackerIsPlayer: false, otherPartyVisible: false);
        Check("A hit from an unseen attacker hides its identity while still reporting severity",
            hitMessage.Contains("Something") && hitMessage.Contains("darkness") && !hitMessage.Contains(monster.Name));

        var missResult = new AttackResult { Attacker = monster, Defender = player, Hit = false, Damage = 0, AttackType = AttackType.Bite };
        string missMessage = CombatMessages.Format(missResult, attackerIsPlayer: false, otherPartyVisible: false);
        Check("A miss from an unseen attacker also hides its identity",
            missMessage.Contains("Something") && !missMessage.Contains(monster.Name));
    }

    private static void PlayerAttackingSomethingUnseenAlsoHidesItsIdentity()
    {
        var rng = new Random(210);
        var player = new Player("HiddenDefenderTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var monster = Monster.CreateRandom(0, 0, 1, rng);

        var hitResult = new AttackResult { Attacker = player, Defender = monster, Hit = true, Damage = 4, Severity = HitSeverity.Minor, AttackType = AttackType.Slash };
        string hitMessage = CombatMessages.Format(hitResult, attackerIsPlayer: true, otherPartyVisible: false);
        Check("The player attacking something unseen doesn't reveal its identity either",
            hitMessage.Contains("something") && !hitMessage.Contains(monster.Name));

        var missResult = new AttackResult { Attacker = player, Defender = monster, Hit = false, Damage = 0, AttackType = AttackType.Slash };
        string missMessage = CombatMessages.Format(missResult, attackerIsPlayer: true, otherPartyVisible: false);
        Check("...and a miss against something unseen reads the same way",
            missMessage.Contains("something") && !missMessage.Contains(monster.Name));
    }

    /// <summary>
    /// Regression test for a real bug report: CombatMessages.Format itself always correctly hid
    /// an unseen combatant's identity when told to (see the two tests just above), but the actual
    /// bump-attack CALLER in GameLoop.HandleMove never computed and passed that flag at all --
    /// it always defaulted to otherPartyVisible: true, so attacking a monster sitting on a
    /// currently-invisible (dark) tile still printed its real name, while the monster's own
    /// attack on the player (via ChaseAI, which DID compute this) correctly said "something."
    /// Exercises the real caller end-to-end rather than the leaf formatting function, since that's
    /// exactly the class of bug (correct function, forgetful caller) unit tests on Format alone
    /// can't catch.
    /// </summary>
    private static void BumpAttackingAnInvisibleOccupantNeverRevealsItsName()
    {
        var rng = new Random(901);
        var player = new Player("DarkBumpTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;

        int targetX = player.X + 1, targetY = player.Y;
        level.Tiles[targetX, targetY] = Tile.CreateFloor();
        level.Tiles[targetX, targetY].IsVisible = false; // simulates an unilluminated dark-room occupant
        level.Actors.RemoveAll(a => a.X == targetX && a.Y == targetY && a is not Player);
        var monster = Monster.CreateRandom(targetX, targetY, 1, rng);
        level.Actors.Add(monster);

        int messagesBefore = gameLoop.StatusMessages.Count;
        gameLoop.HandleMove(level, PlayerCommand.MoveEast);
        var newMessages = gameLoop.StatusMessages.Skip(messagesBefore).ToList();

        Check("Bumping into an invisible occupant produces at least one status message", newMessages.Count > 0);
        Check("None of those messages reveal the invisible occupant's true name",
            !newMessages.Any(m => m.Contains(monster.Name)));
        Check("At least one message uses the generic 'something' wording instead",
            newMessages.Any(m => m.Contains("something", StringComparison.OrdinalIgnoreCase)));
    }

    private static void VisibleAttackerMessagesAreUnaffectedByTheNewParameter()
    {
        var rng = new Random(211);
        var player = new Player("VisibleAttackTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var monster = Monster.CreateRandom(0, 0, 1, rng);

        var result = new AttackResult { Attacker = monster, Defender = player, Hit = true, Damage = 4, Severity = HitSeverity.Minor, AttackType = AttackType.Bite };
        string defaultMessage = CombatMessages.Format(result, attackerIsPlayer: false);
        string explicitVisibleMessage = CombatMessages.Format(result, attackerIsPlayer: false, otherPartyVisible: true);

        Check("Omitting otherPartyVisible (defaulting true) behaves identically to passing true explicitly, naming the attacker either way",
            defaultMessage == explicitVisibleMessage && !defaultMessage.Contains("Something"));
    }

    private static void StairsRoomsAreNeverGeneratedDark()
    {
        var rng = new Random(212);
        bool stairsEverDark = false;
        bool darkRoomEverAppeared = false;
        for (int i = 0; i < 40; i++)
        {
            var level = DungeonGenerator.Generate(floorIndex: 1, width: 60, height: 22, rng, difficultyLevel: 1);
            if (level.Tiles[level.StairsUpPosition.X, level.StairsUpPosition.Y].IsDarkRoom
                || level.Tiles[level.StairsDownPosition.X, level.StairsDownPosition.Y].IsDarkRoom)
            {
                stairsEverDark = true;
            }
            for (int x = 0; x < level.Width && !darkRoomEverAppeared; x++)
            {
                for (int y = 0; y < level.Height && !darkRoomEverAppeared; y++)
                {
                    if (level.Tiles[x, y].IsDarkRoom)
                    {
                        darkRoomEverAppeared = true;
                    }
                }
            }
        }

        Check("Across many generated floors, the stairs-up/stairs-down tile is never generated dark",
            !stairsEverDark);
        Check("...and dark rooms do actually appear somewhere across those floors (the feature isn't silently disabled)",
            darkRoomEverAppeared);
    }

    /// <summary>
    /// Regression check for a real bug report (the SECOND one on this exact topic -- see
    /// PaintDarkRoomTiles' own doc comment for the history): a dark room's bounding walls must
    /// NEVER be marked IsDarkRoom, because a wall tile is shared with whatever's on its other
    /// side -- a wall between a dark room and an ordinary, lit corridor has to stay visible to
    /// someone just walking down that corridor. Scans every generated floor's dark-room interior
    /// tiles for an adjacent wall and confirms that wall is NOT marked dark.
    /// </summary>
    private static void DarkRoomWallsAreNeverMarkedDarkThemselves()
    {
        var rng = new Random(213);
        bool foundADarkRoomWithAdjacentWalls = false;
        bool noAdjacentWallWasEverDark = true;
        var offsets = new[] { (0, -1), (0, 1), (-1, 0), (1, 0) };

        for (int i = 0; i < 40; i++)
        {
            var level = DungeonGenerator.Generate(floorIndex: 1, width: 60, height: 22, rng, difficultyLevel: 1);

            for (int x = 0; x < level.Width; x++)
            {
                for (int y = 0; y < level.Height; y++)
                {
                    if (level.Tiles[x, y].Type != TileType.Floor || !level.Tiles[x, y].IsDarkRoom)
                    {
                        continue;
                    }

                    foreach (var (dx, dy) in offsets)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (!level.IsInBounds(nx, ny) || level.Tiles[nx, ny].Type != TileType.Wall)
                        {
                            continue;
                        }

                        foundADarkRoomWithAdjacentWalls = true;
                        if (level.Tiles[nx, ny].IsDarkRoom)
                        {
                            noAdjacentWallWasEverDark = false;
                        }
                    }
                }
            }
        }

        Check("Test setup sanity check: dark rooms with real wall neighbors actually turn up across many generated floors",
            foundADarkRoomWithAdjacentWalls);
        Check("No wall tile bordering a dark room's interior is itself marked dark -- it stays visible to anyone sighted on either side",
            noAdjacentWallWasEverDark);
    }

    /// <summary>
    /// Companion to the generation-level check above, at the FieldOfView level: a wall shared
    /// between a dark room and an ordinary space must be visible to a SIGHTED observer (someone
    /// NOT currently standing blind), exactly like a door -- and must still be hidden from a
    /// BLIND observer (standing inside the dark room with no light) who's never seen it before,
    /// via FieldOfView.Compute's observer-blindness rule, which doesn't care about the wall's own
    /// IsDarkRoom state at all.
    /// </summary>
    private static void WallBorderingADarkRoomIsVisibleToASightedObserverButNotABlindOne()
    {
        var sightedLevel = BuildOpenLevel(5, 5); // origin (2,2) is ordinary -- not blind
        sightedLevel.Tiles[3, 3] = Tile.CreateWall();
        FieldOfView.Compute(sightedLevel, 2, 2, radius: 8);
        Check("A sighted observer sees a wall (never marked IsDarkRoom) via plain line of sight",
            sightedLevel.Tiles[3, 3].IsVisible);

        var blindLevel = BuildDarkOpenLevel(5, 5); // origin (2,2) is dark and unlit -- blind
        blindLevel.Tiles[3, 3] = Tile.CreateWall();
        FieldOfView.Compute(blindLevel, 2, 2, radius: 8);
        Check("A blind observer (standing unlit inside the dark room) does not see that same never-explored wall",
            !blindLevel.Tiles[3, 3].IsVisible && !blindLevel.Tiles[3, 3].IsExplored);
    }

    /// <summary>
    /// End-to-end scenario matching the "Dark Room Walls, Doors, and Previously Discovered Map
    /// Features" spec's own worked example almost tile-for-tile: a dark room sits above a lit
    /// tunnel, separated by one wall with a door in it; a second, never-approached door sits on
    /// the room's far (left) wall. Exercises the full discovery lifecycle in one place: seeing
    /// the shared wall/door from the lit side, that knowledge persisting once the player is
    /// standing blind inside the room, the room's own floor staying black despite being walked
    /// across, and the never-seen door staying completely unknown throughout.
    ///
    /// Layout (Y grows downward):
    ///   y=0  ##########   outer wall
    ///   y=1  #........#   dark room interior
    ///   y=2  #........#   dark room interior
    ///   y=3  #........#   dark room interior
    ///   y=4  ####+#####   wall separating room from tunnel; KNOWN door at x=4
    ///   y=5  ..........   lit tunnel (ordinary, never dark)
    /// Plus an UNKNOWN door at (0, 2), on the room's own left wall, never approached from either side.
    /// </summary>
    private static void DarkRoomSharedWallAndDoorDiscoveryEndToEndScenario()
    {
        const int width = 10, height = 6;
        var level = new Level(1, width, height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                level.Tiles[x, y] = Tile.CreateWall();
            }
        }

        for (int x = 1; x <= 8; x++)
        {
            for (int y = 1; y <= 3; y++)
            {
                level.Tiles[x, y] = Tile.CreateFloor();
                level.Tiles[x, y].IsDarkRoom = true;
            }
        }

        for (int x = 0; x < width; x++)
        {
            level.Tiles[x, 5] = Tile.CreateFloor(); // the tunnel -- ordinary, never dark
        }

        level.Tiles[4, 4] = Tile.CreateFloor(); // a door's own tile is never dark, even embedded in a wall row
        var knownDoor = new Door(4, 4, isLocked: false, isPickable: true, isBashable: true, difficulty: 10, maxHealth: 20);
        level.Doors.Add(knownDoor);

        level.Tiles[0, 2] = Tile.CreateFloor();
        var unknownDoor = new Door(0, 2, isLocked: false, isPickable: true, isBashable: true, difficulty: 10, maxHealth: 20);
        level.Doors.Add(unknownDoor);

        // --- Step 1: walk the lit tunnel, approaching the known door from below. ---
        FieldOfView.Compute(level, originX: 4, originY: 5, radius: 8);

        Check("From the lit tunnel, the known door is currently visible and becomes discovered",
            level.Tiles[4, 4].IsVisible && level.Tiles[4, 4].IsExplored);
        Check("The wall immediately beside that door (part of the room/tunnel boundary) is also discovered from the tunnel",
            level.Tiles[3, 4].IsVisible && level.Tiles[3, 4].IsExplored);
        Check("The dark room's interior is NOT revealed just because its door is visible from the tunnel",
            !level.Tiles[4, 2].IsVisible && !level.Tiles[4, 2].IsExplored);
        Check("The unknown door on the room's far wall is nowhere near discovered yet either",
            !level.Tiles[0, 2].IsVisible && !level.Tiles[0, 2].IsExplored);

        // --- Step 2: the door opens (as HandleDoorBump would do) and the player steps through
        // into the room with no light source -- they are now blind. ---
        knownDoor.IsOpen = true;
        FieldOfView.Compute(level, originX: 4, originY: 2, radius: 8); // standing just inside, unlit

        Check("Entering the dark room without light does not reveal its floor, even standing right in it",
            !level.Tiles[6, 2].IsVisible && !level.Tiles[6, 2].IsExplored);
        Check("The previously-discovered door remains known (remembered dim) even though the player is now blind",
            level.Tiles[4, 4].IsExplored && !level.Tiles[4, 4].IsVisible);
        Check("The previously-discovered wall beside it remains known too, for the same reason",
            level.Tiles[3, 4].IsExplored && !level.Tiles[3, 4].IsVisible);
        Check("The never-seen door on the room's far wall stays completely hidden even now that the player is inside the room",
            !level.Tiles[0, 2].IsVisible && !level.Tiles[0, 2].IsExplored);

        // --- Step 3: walk across the room's own dark floor -- movement alone still never
        // discovers it. ---
        FieldOfView.Compute(level, originX: 7, originY: 2, radius: 8); // moved further in, still unlit
        Check("Walking across (not just standing near) unlit dark-room floor never discovers it",
            !level.Tiles[7, 2].IsVisible && !level.Tiles[7, 2].IsExplored);
    }

    /// <summary>
    /// Regression check for a real bug report: standing in a lit corridor right at a dark room's
    /// entrance, a sighted player could see clean across the room's open (geometrically
    /// transparent) unlit interior to a wall or door on its FAR side, since neither the interior
    /// floor nor the far boundary is itself opaque to shadowcasting -- only the near boundary is
    /// meant to be visible from a lit approach (see WallBorderingADarkRoomIsVisibleToASightedObserverButNotABlindOne),
    /// not the room's entire far wall glimpsed straight through the darkness in between. Fixed by
    /// having a SIGHTED observer's own shadowcast (not a blind one's -- see FieldOfView.Compute's
    /// own doc comment) treat an unlit dark-room tile as opaque, stopping the ray at the first one
    /// it meets instead of only hiding it while letting sight continue past it.
    /// </summary>
    private static void ASightedObserverCannotSeeAcrossADarkRoomsOpenInteriorToItsFarBoundary()
    {
        const int width = 10, height = 3;
        var level = new Level(1, width, height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                level.Tiles[x, y] = Tile.CreateWall();
            }
        }

        level.Tiles[0, 1] = Tile.CreateFloor(); // lit corridor tile -- the player's origin
        level.Tiles[1, 1] = Tile.CreateFloor(); // the room's near door/boundary -- never dark itself
        for (int x = 2; x <= 7; x++)
        {
            level.Tiles[x, 1] = Tile.CreateFloor();
            level.Tiles[x, 1].IsDarkRoom = true; // the room's own unlit interior
        }
        // level.Tiles[8, 1] is left as Tile.CreateWall() from the fill above -- the room's far wall.

        FieldOfView.Compute(level, originX: 0, originY: 1, radius: 15);

        Check("The near door/boundary tile is still visible and discovered from the lit corridor, exactly as before this fix",
            level.Tiles[1, 1].IsVisible && level.Tiles[1, 1].IsExplored);
        Check("The dark room's own interior floor is not revealed, same as always",
            !level.Tiles[2, 1].IsVisible && !level.Tiles[2, 1].IsExplored);
        Check("The wall on the room's FAR side is not revealed just because geometry could otherwise reach it straight through the unlit interior",
            !level.Tiles[8, 1].IsVisible && !level.Tiles[8, 1].IsExplored);
    }

    private static Level BuildOpenLevelWithWalls(int width, int height, IEnumerable<(int X, int Y)> wallPositions)
    {
        var level = new Level(1, width, height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                level.Tiles[x, y] = Tile.CreateFloor();
            }
        }
        foreach (var (x, y) in wallPositions)
        {
            level.Tiles[x, y] = Tile.CreateWall();
        }
        return level;
    }

    /// <summary>Basic FOV sanity check: a fully open room has no geometry to hide anything, so every tile within the radius must be visible and nothing beyond it should be. Exercises FieldOfView.ComputeVisibleCells' FilterToConnected post-pass too -- an open room's connectivity chain from the origin is trivially unbroken.</summary>
    private static void FovSeesEveryOpenTileWithinRadiusAndNothingBeyondIt()
    {
        var level = BuildOpenLevelWithWalls(21, 21, Array.Empty<(int, int)>());
        var visible = FieldOfView.ComputeVisibleCells(level, originX: 10, originY: 10, radius: 5);

        bool everyTileWithinRadiusVisible = true;
        bool nothingBeyondRadiusVisible = true;
        for (int x = 0; x < 21; x++)
        {
            for (int y = 0; y < 21; y++)
            {
                int dx = x - 10, dy = y - 10;
                bool withinRadius = (dx * dx) + (dy * dy) <= 25;
                bool isVisible = visible.Contains((x, y));
                if (withinRadius && !isVisible) everyTileWithinRadiusVisible = false;
                if (!withinRadius && isVisible) nothingBeyondRadiusVisible = false;
            }
        }

        Check("Every unobstructed tile within the radius is visible", everyTileWithinRadiusVisible);
        Check("Nothing beyond the radius is visible", nothingBeyondRadiusVisible);
    }

    /// <summary>A single wall directly on the line between the observer and a target blocks the target and everything past it, but the wall tile itself is still seen (looking straight at a wall shows the wall).</summary>
    private static void FovStopsAtAWallButStillShowsTheWallItself()
    {
        var level = BuildOpenLevelWithWalls(7, 3, new[] { (3, 1) }); // a single wall tile directly east of the origin
        var visible = FieldOfView.ComputeVisibleCells(level, originX: 0, originY: 1, radius: 6);

        Check("Tiles between the origin and the wall are visible", visible.Contains((1, 1)) && visible.Contains((2, 1)));
        Check("The wall tile itself is visible", visible.Contains((3, 1)));
        Check("Tiles directly beyond the wall are not visible", !visible.Contains((4, 1)) && !visible.Contains((5, 1)));
    }

    /// <summary>
    /// Regression check for the "orphaned wall" report FilterToConnected exists to fix: a fully
    /// enclosed side room with NO gap at all in its boundary must never leak any visibility to its
    /// interior, no matter how close a diagonal corner of that boundary comes to the observer's
    /// own line of sight. Recursive shadowcasting's slope-interval sweep can, without this filter,
    /// occasionally mark a tile visible through a tight diagonal corner without ever validating
    /// (or marking explored) the tile actually connecting it back to the observer -- the exact
    /// "keyhole" artifact. FilterToConnected prunes anything without an unbroken chain of
    /// already-visible tiles back to the origin, which a fully sealed room (zero actual gap) can
    /// never have. This test uses a completely sealed room as the strongest possible version of
    /// that guarantee: zero leakage where shadowcasting's approximation has the most room to go
    /// wrong.
    /// </summary>
    private static void FovConnectivityFilterNeverLeaksIntoAFullyEnclosedRoomThroughATightCorner()
    {
        // A 3x3 sealed room (interior at (5,5)) sitting diagonally adjacent, corner-to-corner, to
        // the observer's own open area -- the tightest possible corner pinch with zero actual gap.
        var walls = new List<(int, int)>();
        for (int x = 4; x <= 6; x++)
        {
            walls.Add((x, 4));
            walls.Add((x, 6));
        }
        for (int y = 4; y <= 6; y++)
        {
            walls.Add((4, y));
            walls.Add((6, y));
        }

        var level = BuildOpenLevelWithWalls(11, 11, walls);
        var visible = FieldOfView.ComputeVisibleCells(level, originX: 3, originY: 3, radius: 10);

        Check("The sealed room's own interior is never visible from outside it, even diagonally corner-adjacent to the observer",
            !visible.Contains((5, 5)));
        Check("The sealed room's far wall (directly across its own interior) is never visible either",
            !visible.Contains((5, 6)) && !visible.Contains((6, 5)));
    }

    /// <summary>
    /// Regression check for a real "orphaned wall" report: a genuinely disconnected floor pocket
    /// (no door, no corridor -- a bona fide generation-time or hidden-door-not-yet-revealed gap,
    /// not just a keyhole sight leak) whose walls could still get marked visible/explored by raw
    /// shadowcasting despite the room itself being physically unreachable. Level.RecomputeReachability
    /// is the structural fix: it floods out from StairsUpPosition across real walkable terrain and
    /// only marks a wall reachable if it borders a tile that flood actually reached, independent of
    /// whatever FOV/lighting's own sight-line math would otherwise compute.
    /// </summary>
    private static void RecomputeReachabilityMarksOnlyTheConnectedComponentAndItsBorderingWalls()
    {
        var level = new Level(1, 11, 7); // starts fully walled -- see Level's own constructor.
        for (int x = 1; x <= 4; x++)
        {
            for (int y = 1; y <= 4; y++)
            {
                level.Tiles[x, y] = Tile.CreateFloor();
            }
        }
        for (int x = 7; x <= 9; x++) // a second floor pocket, columns 5-6 left as solid wall -- no connection at all.
        {
            for (int y = 1; y <= 4; y++)
            {
                level.Tiles[x, y] = Tile.CreateFloor();
            }
        }
        level.StairsUpPosition = (2, 2); // inside the main room.
        level.RecomputeReachability();

        Check("Every floor tile in the main (stairs-connected) room is reachable",
            level.Tiles[2, 2].IsReachable && level.Tiles[4, 4].IsReachable && level.Tiles[1, 1].IsReachable);
        Check("Every floor tile in the disconnected pocket is NOT reachable, even though it's ordinary open floor",
            !level.Tiles[7, 2].IsReachable && !level.Tiles[9, 4].IsReachable);
        Check("The main room's own far wall (bordering its reachable floor) still renders",
            level.Tiles[5, 2].IsReachable);
        Check("The disconnected pocket's near wall (bordering only unreachable floor) stays hidden",
            !level.Tiles[6, 2].IsReachable);
        Check("The disconnected pocket's outer wall (bordering only unreachable floor) also stays hidden",
            !level.Tiles[10, 2].IsReachable);
    }

    /// <summary>
    /// The actual user-facing fix: even when FieldOfView's own shadowcast+keyhole-filter pipeline
    /// would otherwise return a tile (here, forced by using a radius and origin large/close enough
    /// that the disconnected pocket's near wall sits directly in an unobstructed sight line -- no
    /// keyhole diagonal needed at all), ComputeVisibleCells must still never return an unreachable
    /// tile once Level.RecomputeReachability has run.
    /// </summary>
    private static void FieldOfViewNeverReturnsAnUnreachableTileRegardlessOfRawLineOfSight()
    {
        // A single long corridor -- ordinary, unobstructed floor -- leading to a "pocket" that is
        // floor-connected all the way (so raw shadowcasting sees straight down it with no keyhole
        // involved at all) but whose STAIRS-UP-anchored reachability was deliberately never
        // extended past x=5, simulating a real orphaned segment (e.g. one only reachable via a
        // still-undiscovered hidden door elsewhere).
        var level = new Level(1, 15, 3);
        for (int x = 1; x <= 12; x++)
        {
            level.Tiles[x, 1] = Tile.CreateFloor();
        }
        level.StairsUpPosition = (1, 1);

        // Manually mark reachability as if only x=1..5 is actually connected -- bypassing
        // RecomputeReachability's own flood fill so this test exercises ComputeVisibleCells'
        // gating in isolation, independent of whether the flood fill itself is correct (that's
        // RecomputeReachabilityMarksOnlyTheConnectedComponentAndItsBorderingWalls' job).
        for (int x = 0; x < level.Width; x++)
        {
            level.Tiles[x, 1].IsReachable = x <= 5;
        }

        var visible = FieldOfView.ComputeVisibleCells(level, originX: 1, originY: 1, radius: 20);

        Check("An ordinary, unobstructed tile within the reachable segment is visible",
            visible.Contains((5, 1)));
        Check("A tile with a fully open, unobstructed line of sight is still never returned once it's marked unreachable -- the reachability gate overrides raw shadowcasting entirely",
            !visible.Contains((6, 1)) && !visible.Contains((10, 1)));
    }

    /// <summary>Confirms LightingSystem's illumination pass funnels through the same ComputeVisibleCells gate -- a light source must not illuminate a structurally unreachable pocket either, matching FieldOfView's own behavior.</summary>
    private static void LightingNeverIlluminatesAnUnreachableTile()
    {
        var level = new Level(1, 10, 3);
        for (int x = 1; x <= 8; x++)
        {
            level.Tiles[x, 1] = Tile.CreateFloor();
        }
        level.StairsUpPosition = (1, 1);
        for (int x = 0; x < level.Width; x++)
        {
            level.Tiles[x, 1].IsReachable = x <= 4;
        }

        var lit = FieldOfView.ComputeVisibleCells(level, originX: 1, originY: 1, radius: 20);

        Check("A light source's reach still respects the reachability gate, exactly like the player's own FOV",
            lit.Contains((4, 1)) && !lit.Contains((6, 1)) && !lit.Contains((8, 1)));
    }

    /// <summary>Regression guard for the retroactive cleanup: a save written while the "orphaned wall" bug was live could already have IsExplored = true baked in for an unreachable tile -- loading it must forget that memory, not just stop it from happening again going forward.</summary>
    private static void OldSaveWithAnAlreadyExploredOrphanedWallForgetsItOnLoad()
    {
        var tileTypes = new TileType[11 * 7];
        var tileExplored = new bool[11 * 7];
        for (int x = 0; x < 11; x++)
        {
            for (int y = 0; y < 7; y++)
            {
                int i = x * 7 + y;
                bool isMainRoomFloor = x is >= 1 and <= 4 && y is >= 1 and <= 4;
                bool isPocketFloor = x is >= 7 and <= 9 && y is >= 1 and <= 4;
                tileTypes[i] = isMainRoomFloor || isPocketFloor ? TileType.Floor : TileType.Wall;
                // Simulates the bug already having happened: the disconnected pocket and its
                // walls were wrongly marked Explored on some earlier, buggy turn.
                tileExplored[i] = true;
            }
        }

        var levelData = new LevelData
        {
            FloorIndex = 1,
            Width = 11,
            Height = 7,
            StairsUpX = 2,
            StairsUpY = 2,
            StairsDownX = 2,
            StairsDownY = 2,
            TileTypes = tileTypes,
            TileExplored = tileExplored,
            Monsters = new List<MonsterData>(),
            Traders = new List<TraderData>(),
            GroundItems = new List<GroundItemData>(),
            Chests = new List<ChestData>(),
            Traps = new List<TrapData>(),
            Doors = new List<DoorData>()
        };

        var restored = SaveManager.FromLevelData(levelData);

        Check("The main (stairs-connected) room's remembered exploration survives the load, exactly as before",
            restored.Tiles[2, 2].IsExplored);
        Check("The disconnected pocket's wrongly-remembered exploration is forgotten on load, since it isn't actually reachable",
            !restored.Tiles[8, 2].IsExplored);
    }

    /// <summary>
    /// Regression check for a real bug report: a dark room could spawn with one entrance behind
    /// a door and another bare (no door at all) -- walking in through the doored entrance let
    /// line of sight run straight through the bare one and down whatever corridor it led to,
    /// lighting up an undiscovered exit from clear across an otherwise-black room. Scans every
    /// generated floor for a dark room's own edge floor tile bordering a non-dark floor tile
    /// (i.e. a genuine entrance, per FindRoomEntrance's "one tile outside the rectangle"
    /// convention) and confirms a Door object sits at that neighboring position -- locked or
    /// unlocked doesn't matter, only that SOME door is there to block the corridor beyond.
    /// </summary>
    private static void EveryDarkRoomEntranceHasADoor()
    {
        var rng = new Random(214);
        bool foundADarkRoomEntranceToCheck = false;
        bool everyDarkRoomEntranceHasADoor = true;
        var offsets = new[] { (0, -1), (0, 1), (-1, 0), (1, 0) };

        for (int i = 0; i < 40; i++)
        {
            var level = DungeonGenerator.Generate(floorIndex: 1, width: 60, height: 22, rng, difficultyLevel: 1);

            for (int x = 0; x < level.Width; x++)
            {
                for (int y = 0; y < level.Height; y++)
                {
                    if (level.Tiles[x, y].Type != TileType.Floor || !level.Tiles[x, y].IsDarkRoom)
                    {
                        continue;
                    }

                    foreach (var (dx, dy) in offsets)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (!level.IsInBounds(nx, ny))
                        {
                            continue;
                        }

                        var neighbor = level.Tiles[nx, ny];
                        if (neighbor.Type != TileType.Floor || neighbor.IsDarkRoom)
                        {
                            continue; // not a boundary crossing into a corridor/other room
                        }

                        foundADarkRoomEntranceToCheck = true;
                        if (!level.Doors.Any(d => d.X == nx && d.Y == ny))
                        {
                            everyDarkRoomEntranceHasADoor = false;
                        }
                    }
                }
            }
        }

        Check("Test setup sanity check: dark-room entrances actually turn up across many generated floors",
            foundADarkRoomEntranceToCheck);
        Check("Every entrance to a dark room has a door -- never a bare opening that would leak line of sight into the corridor beyond",
            everyDarkRoomEntranceHasADoor);
    }

    /// <summary>
    /// Regression check for the actual root cause behind the bug report above: TrySpawnBossRoom
    /// picks a random existing room as its spur's "host" and only ever doors its OWN (boss-room)
    /// end of that corridor -- if the host happened to be a dark room, its boundary was left
    /// completely open with an undoored corridor leading toward the (distant, doored) boss room.
    /// Keeps rolling fresh seeds until one produces a floor with both a boss room and at least
    /// one dark room, then re-runs the same "every dark-room entrance has a door" scan against
    /// that specific floor.
    /// </summary>
    private static void BossRoomSpurNeverLeavesADarkRoomHostUndoored()
    {
        var offsets = new[] { (0, -1), (0, 1), (-1, 0), (1, 0) };

        for (int seed = 0; seed < 300; seed++)
        {
            var rng = new Random(seed);
            var level = DungeonGenerator.Generate(floorIndex: 10, width: 60, height: 22, rng, difficultyLevel: 10);

            bool hasBoss = level.Actors.OfType<Monster>().Any(m => m.IsBoss);
            bool hasDarkRoom = false;
            for (int x = 0; x < level.Width && !hasDarkRoom; x++)
            {
                for (int y = 0; y < level.Height && !hasDarkRoom; y++)
                {
                    if (level.Tiles[x, y].Type == TileType.Floor && level.Tiles[x, y].IsDarkRoom)
                    {
                        hasDarkRoom = true;
                    }
                }
            }

            if (!hasBoss || !hasDarkRoom)
            {
                continue;
            }

            bool everyEntranceHasADoor = true;
            for (int x = 0; x < level.Width; x++)
            {
                for (int y = 0; y < level.Height; y++)
                {
                    if (level.Tiles[x, y].Type != TileType.Floor || !level.Tiles[x, y].IsDarkRoom)
                    {
                        continue;
                    }

                    foreach (var (dx, dy) in offsets)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (!level.IsInBounds(nx, ny))
                        {
                            continue;
                        }

                        var neighbor = level.Tiles[nx, ny];
                        if (neighbor.Type == TileType.Floor && !neighbor.IsDarkRoom && !level.Doors.Any(d => d.X == nx && d.Y == ny))
                        {
                            everyEntranceHasADoor = false;
                        }
                    }
                }
            }

            Check("A floor with both a boss room and a dark room still has a door at every one of that dark room's entrances -- the boss spur never left one open",
                everyEntranceHasADoor);
            return;
        }

        Skip("Boss room + dark room interaction", "300 seeds rolled and none produced a floor with both -- can't exercise this specific interaction");
    }

    // --- Room Objects -----------------------------------------------------------------

    private static void RoomObjectBlocksActorMovementOnlyWhenBlocksMovementIsTrue()
    {
        var level = BuildOpenLevel(5, 5);
        var blocking = new RoomObject(2, 2, RoomObjectType.Boulder, "test boulder", "short", "long",
            'O', ConsoleColor.Gray, blocksMovement: true, blocksVision: false, blocksProjectiles: false, isMovable: false);
        level.RoomObjects.Add(blocking);
        Check("A movement-blocking RoomObject makes its own tile blocked for actor movement",
            level.IsBlockedForActorMovement(2, 2));

        level.RoomObjects.Clear();
        var nonBlocking = new RoomObject(2, 2, RoomObjectType.WaterFountain, "test fountain", "short", "long",
            '0', ConsoleColor.Cyan, blocksMovement: false, blocksVision: false, blocksProjectiles: false, isMovable: false);
        level.RoomObjects.Add(nonBlocking);
        Check("A RoomObject with BlocksMovement=false leaves its tile open for actor movement -- the flag is genuinely per-instance, not hardcoded by type",
            !level.IsBlockedForActorMovement(2, 2));
    }

    /// <summary>Exercises every kind of destination the design spec calls "ambiguous" for a pushed/generated object -- map boundary, wall, another actor, a closed door, a chest, an item, a trap, the stairs, and another room object -- all in one pass.</summary>
    private static void IsBlockedForObjectPlacementRejectsEveryAmbiguousDestination()
    {
        var level = BuildOpenLevel(10, 3);
        level.Tiles[1, 1] = Tile.CreateWall();
        level.StairsUpPosition = (9, 9); // off this row entirely; only the explicit stairs checks below matter
        level.StairsDownPosition = (2, 1);

        var actor = Monster.Restore(BaseRestoreData("placement-actor", attack: 0, defense: 0));
        actor.X = 3;
        actor.Y = 1;
        level.Actors.Add(actor);

        level.Doors.Add(new Door(4, 1, isLocked: false, isPickable: true, isBashable: true, difficulty: 0, maxHealth: 20));
        level.Chests.Add(new Chest(5, 1, isLocked: false, difficulty: 0, new List<Item>()));
        level.AddItem(6, 1, Items.HealthPotion.Clone());
        level.Traps.Add(new Trap(7, 1, damage: 1));
        level.RoomObjects.Add(new RoomObject(8, 1, RoomObjectType.Shrine, "other shrine", "s", "l",
            'n', ConsoleColor.White, blocksMovement: true, blocksVision: false, blocksProjectiles: false, isMovable: false));

        Check("Map boundary is blocked for object placement", level.IsBlockedForObjectPlacement(-1, 1));
        Check("A wall tile is blocked for object placement", level.IsBlockedForObjectPlacement(1, 1));
        Check("The stairs-down tile is blocked for object placement", level.IsBlockedForObjectPlacement(2, 1));
        Check("A tile occupied by an actor is blocked for object placement", level.IsBlockedForObjectPlacement(3, 1));
        Check("A closed door's tile is blocked for object placement", level.IsBlockedForObjectPlacement(4, 1));
        Check("A chest's tile is blocked for object placement", level.IsBlockedForObjectPlacement(5, 1));
        Check("An item's tile is blocked for object placement", level.IsBlockedForObjectPlacement(6, 1));
        Check("A trap's tile is blocked for object placement", level.IsBlockedForObjectPlacement(7, 1));
        Check("Another room object's tile is blocked for object placement", level.IsBlockedForObjectPlacement(8, 1));
        Check("A genuinely open floor tile is NOT blocked for object placement", !level.IsBlockedForObjectPlacement(0, 1));
    }

    /// <summary>
    /// Locks in the deliberate difference between the two centralized occupancy checks: ordinary
    /// actor movement tolerates walking onto a trap/item (triggering/picking up is a separate
    /// concern), but a pushed object or a generation placement must reject those as ambiguous.
    /// A chest is the one exception -- per the Persistent Containers proposal's own explicit
    /// recommendation, a chest blocks actor movement in both checks (the physical chest remains
    /// present whether open or closed), unlike the pre-existing "always walkable" behavior.
    /// </summary>
    private static void IsBlockedForActorMovementToleratesTrapsAndItemsButNotChestsThatObjectPlacementRejects()
    {
        var level = BuildOpenLevel(6, 3);
        level.Chests.Add(new Chest(1, 1, isLocked: false, difficulty: 0, new List<Item>()));
        level.Traps.Add(new Trap(2, 1, damage: 1));
        level.AddItem(3, 1, Items.HealthPotion.Clone());

        Check("A chest now blocks ordinary actor movement -- a deliberate behavior change (see Dungeon/Chest.cs)", level.IsBlockedForActorMovement(1, 1));
        Check("Ordinary actor movement tolerates walking onto a trap's tile", !level.IsBlockedForActorMovement(2, 1));
        Check("Ordinary actor movement tolerates walking onto an item's tile", !level.IsBlockedForActorMovement(3, 1));
        Check("Object placement rejects the same chest tile", level.IsBlockedForObjectPlacement(1, 1));
        Check("Object placement rejects the same trap tile", level.IsBlockedForObjectPlacement(2, 1));
        Check("Object placement rejects the same item tile", level.IsBlockedForObjectPlacement(3, 1));
    }

    // --- Persistent Containers and Magical Bags --------------------------------------------

    private static void ChestCanBeOpenedClosedAndReopenedRemainingFindable()
    {
        var rng = new Random(920);
        var player = new Player("ChestToggleTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        level.Chests.Clear();
        level.GroundItems.RemoveAll(g => g.X == player.X && g.Y == player.Y);

        var chest = new Chest(player.X, player.Y, isLocked: false, difficulty: 0, new List<Item> { Items.HealthPotion.Clone() });
        level.Chests.Add(chest);

        Check("A fresh chest is found by GetChestAt", level.GetChestAt(player.X, player.Y) == chest);

        bool openedTurnConsumed = gameLoop.HandleOpenChest(level, showScreen: false);
        Check("Opening a chest consumes a turn", openedTurnConsumed);
        Check("Opening sets IsOpen", chest.IsOpen);
        Check("The chest is STILL found by GetChestAt after opening -- unlike the old one-shot behavior", level.GetChestAt(player.X, player.Y) == chest);
        Check("Opening no longer spills contents onto the floor", level.GetItemsAt(player.X, player.Y).Count == 0);
        Check("Contents remain inside the chest's own container", chest.Container.Contents.Items.Count == 1);

        bool closedTurnConsumed = gameLoop.HandleOpenChest(level, showScreen: false);
        Check("Re-interacting with an open chest closes it and consumes a turn", closedTurnConsumed);
        Check("Closing clears IsOpen", !chest.IsOpen);
        Check("Contents survive closing", chest.Container.Contents.Items.Count == 1);

        bool reopenedTurnConsumed = gameLoop.HandleOpenChest(level, showScreen: false);
        Check("Reopening a closed chest works again and consumes a turn", reopenedTurnConsumed && chest.IsOpen);
    }

    private static void SuccessfullyPickedChestLockStaysUnlockedPermanently()
    {
        var rng = new Random(921);
        var player = new Player("LockPickTester", CharacterClass.Thief, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Thief, rng));
        player.KnownSkills.Add(SkillCatalog.PickLock);
        player.Inventory.AddItem(Items.Lockpicks.Clone());
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        level.Chests.Clear();

        // Difficulty 0 -- the roll (minimum 1) can never fall below it, guaranteeing success
        // deterministically without needing to control GameLoop's own internal, unseeded rng.
        var chest = new Chest(player.X, player.Y, isLocked: true, difficulty: 0, new List<Item>());
        level.Chests.Add(chest);

        bool opened = gameLoop.HandleOpenChest(level, showScreen: false);
        Check("A guaranteed-success lock pick opens the chest and consumes a turn", opened && chest.IsOpen);
        Check("The chest becomes permanently unlocked", !chest.IsLocked);

        gameLoop.HandleOpenChest(level, showScreen: false); // close it
        bool reopened = gameLoop.HandleOpenChest(level, showScreen: false); // reopen -- must not ask to pick the lock again
        Check("Reopening an already-unlocked chest never re-prompts for lock picking", reopened && chest.IsOpen && !chest.IsLocked);
    }

    private static void ContainerRulesEnforceSizeEligibilityAndScrollSpellbookException()
    {
        var smallBag = new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Small, 4, 0);
        var mediumBag = new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Medium, 4, 0);
        var largeBag = new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Large, 4, 0);

        var verySmallItem = new Item("Test Ring", '=', "test", ItemType.Armor, itemSize: ItemSize.VerySmall);
        var mediumItem = new Item("Test Medium Item", '?', "test", ItemType.Armor, itemSize: ItemSize.Medium);
        var largeItem = new Item("Test Large Item", '?', "test", ItemType.Armor, itemSize: ItemSize.Large);
        var largeScroll = new Item("Test Large Scroll", '?', "test", ItemType.Scroll, itemSize: ItemSize.Large);

        Check("Small bag accepts a Very Small item", ContainerRules.CanInsert(smallBag, verySmallItem, out _));
        Check("Small bag rejects a Medium item", !ContainerRules.CanInsert(smallBag, mediumItem, out _));
        Check("Medium bag accepts a Medium item", ContainerRules.CanInsert(mediumBag, mediumItem, out _));
        Check("Medium bag rejects an ordinary Large item", !ContainerRules.CanInsert(mediumBag, largeItem, out _));
        Check("Medium bag accepts a Large-sized Scroll via the explicit exception", ContainerRules.CanInsert(mediumBag, largeScroll, out _));
        Check("Large bag accepts a Large item", ContainerRules.CanInsert(largeBag, largeItem, out _));
    }

    private static void ContainerRulesRejectNestingAtEveryLevel()
    {
        var outerBag = new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Large, 8, 0);
        var innerBagItem = new Item("Test Inner Bag", '(', "test", ItemType.Container, itemSize: ItemSize.Small,
            container: new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Small, 4, 0));
        Check("A bag can never be placed inside another bag", !ContainerRules.CanInsert(outerBag, innerBagItem, out string reason) && reason != "");

        var chest = new ContainerComponent(ContainerKind.Chest, default, ChestConfig.DefaultSlotCapacity, 0);
        Check("A chest CAN accept a bag", ContainerRules.CanInsert(chest, innerBagItem, out _));

        // A bag placed inside a chest is still just a PortableBag internally -- inserting into IT
        // (regardless of the fact it now sits inside a chest) must still reject another bag.
        var anotherBagItem = new Item("Test Another Bag", '(', "test", ItemType.Container, itemSize: ItemSize.Small,
            container: new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Small, 4, 0));
        Check("A bag inside a chest still rejects another bag being placed inside IT",
            !ContainerRules.CanInsert(innerBagItem.Container, anotherBagItem, out _));
    }

    private static void ContainerWeightCalculatorMatchesTheProposalsWorkedExamples()
    {
        var heavyContent = new Item("Test Heavy Content", '?', "test", ItemType.Consumable, weight: 20.0);

        var bag25 = new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Large, 4, 0.25);
        var bagItem25 = new Item("Test Bag 25", '(', "test", ItemType.Container, weight: 2.0, container: bag25);
        bagItem25.Container.Contents.AddItem(heavyContent);
        Check("25% reduction: 2 + (20 x 0.75) = 17 lbs", ContainerWeightCalculator.EffectiveWeight(bagItem25) == 17.0);

        var heavyContent2 = new Item("Test Heavy Content 2", '?', "test", ItemType.Consumable, weight: 20.0);
        var bag75 = new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Large, 4, 0.75);
        var bagItem75 = new Item("Test Bag 75", '(', "test", ItemType.Container, weight: 2.0, container: bag75);
        bagItem75.Container.Contents.AddItem(heavyContent2);
        Check("75% reduction: 2 + (20 x 0.25) = 7 lbs", ContainerWeightCalculator.EffectiveWeight(bagItem75) == 7.0);

        var emptyBagNoReduction = new Item("Test Empty Bag", '(', "test", ItemType.Container, weight: 2.0,
            container: new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Large, 4, 0.75));
        Check("A bag's own weight is never itself reduced, even at 75% -- only its contents are",
            ContainerWeightCalculator.EffectiveWeight(emptyBagNoReduction) == 2.0);
    }

    private static void SlotCountingTreatsAMergedStackAsOneSlotAndAPartialRemainderAsNeedingAFreeOne()
    {
        var rng = new Random(922);
        var player = new Player("SlotCountTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var bagContainer = new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Small, 2, 0);

        var existingStack = Items.HealthPotion.Clone();
        existingStack.Charges = 2;
        bagContainer.Contents.AddItem(existingStack);

        var morePotions = Items.HealthPotion.Clone();
        morePotions.Charges = 2;
        player.Inventory.AddItem(morePotions);

        var putResult = ContainerTransferService.TryPut(player, bagContainer, morePotions, 2);
        Check("Merging into an existing compatible stack uses no additional slot", putResult.Success && bagContainer.Contents.Items.Count == 1);
        Check("The merged stack's charges combine correctly", bagContainer.Contents.Items[0].Charges == 4);

        var sword = Items.ShortSword.Clone();
        sword.IsIdentified = true;
        player.Inventory.AddItem(sword);
        var putSwordResult = ContainerTransferService.TryPut(player, bagContainer, sword);
        Check("A different, non-stackable item takes the one remaining free slot", putSwordResult.Success && bagContainer.Contents.Items.Count == 2);

        var dagger = Items.Dagger.Clone();
        dagger.IsIdentified = true;
        player.Inventory.AddItem(dagger);
        var putDaggerResult = ContainerTransferService.TryPut(player, bagContainer, dagger);
        Check("A full container rejects a further distinct item needing its own slot",
            !putDaggerResult.Success && bagContainer.Contents.Items.Count == 2 && player.Inventory.Items.Contains(dagger));
    }

    private static void ContainerTransferServiceBlocksRemovalThatWouldExceedCapacityButNeverBlocksPutting()
    {
        var rng = new Random(923);
        var player = new Player("CapacityTransferTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        double maxCapacity = EncumbranceCalculator.MaxCapacity(player);

        var bagContainer = new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Large, 4, 0.5);
        var heavyItem = new Item("Test Boulder", '*', "test", ItemType.Consumable, weight: maxCapacity);
        bagContainer.Contents.AddItem(heavyItem);
        var bagOwnerItem = new Item("Test Heavy Bag Owner", '(', "test", ItemType.Container, weight: 0.5, container: bagContainer);
        player.Inventory.AddItem(bagOwnerItem);

        var removeResult = ContainerTransferService.TryRemove(player, bagContainer, heavyItem, "Test Heavy Bag Owner");
        Check("Removing an item from a magical bag is blocked when it would exceed carry capacity, naming the bag and item",
            !removeResult.Success && removeResult.Message.Contains("carrying capacity") && bagContainer.Contents.Items.Contains(heavyItem));

        var freshBag = new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Large, 4, 0);
        var freshHeavyItem = new Item("Test Boulder 2", '*', "test", ItemType.Consumable, weight: maxCapacity * 2);
        player.Inventory.AddItem(freshHeavyItem);
        var putResult = ContainerTransferService.TryPut(player, freshBag, freshHeavyItem);
        Check("Putting even a very heavy item into a bag is never blocked by capacity -- it can only reduce or preserve total weight", putResult.Success);
    }

    private static void PickingUpAFilledBagUsesItsCompleteEffectiveWeight()
    {
        var rng = new Random(924);
        var player = new Player("BagPickupTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        level.GroundItems.RemoveAll(g => g.X == player.X && g.Y == player.Y);

        double maxCapacity = EncumbranceCalculator.MaxCapacity(player);
        var bagContainer = new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Large, 4, 0);
        var heavyContent = new Item("Test Anvil", '*', "test", ItemType.Consumable, weight: maxCapacity);
        bagContainer.Contents.AddItem(heavyContent);
        var bagItem = new Item("Test Heavy Bag", '(', "test", ItemType.Container, weight: 1.0, container: bagContainer);
        level.AddItem(player.X, player.Y, bagItem);

        bool pickedUp = gameLoop.HandlePickUp(level, offerEquip: false);
        Check("Picking up a filled bag checks its COMPLETE effective weight, not just the empty bag's own weight",
            !pickedUp && !player.Inventory.Items.Contains(bagItem));

        var emptyBagOfSameWeight = new Item("Test Empty Bag Alone", '(', "test", ItemType.Container, weight: 1.0,
            container: new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Large, 4, 0));
        Check("The bag alone (no contents) would fit under capacity -- confirms the rejection above is really about the contents",
            EncumbranceCalculator.CanCarry(player, emptyBagOfSameWeight, out _));
    }

    private static void DestroyingAFlammableBagSpillsSurvivingContentsWithoutDuplication()
    {
        var level = BuildOpenLevel(6, 3);
        level.Tiles[2, 1].FloorType = FloorType.Fire;

        var survivingContent = Items.HealthPotion.Clone(); // Consumable -- not flammable by default
        var burningContent = new Item("Test Paper Scroll", '?', "test", ItemType.Scroll, weight: 0.1); // Scroll defaults to Flammable: true
        var bagContainer = new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Medium, 4, 0);
        bagContainer.Contents.AddItem(survivingContent);
        bagContainer.Contents.AddItem(burningContent);
        var flammableBag = new Item("Test Cloth Bag", '(', "test", ItemType.Container, weight: 0.5, flammable: true, container: bagContainer);

        string message = ItemDestructionRules.LandOnFloor(level, 2, 1, flammableBag);

        Check("The bag itself is reported destroyed", message != null && message.Contains("Test Cloth Bag"));
        Check("The bag itself never lands on the floor", !level.GetItemsAt(2, 1).Contains(flammableBag));
        Check("The non-flammable content survives and lands on the floor", level.GetItemsAt(2, 1).Contains(survivingContent));
        Check("The flammable content is also destroyed rather than landing", !level.GetItemsAt(2, 1).Contains(burningContent));
        Check("No duplication -- exactly one surviving item lands on this tile", level.GetItemsAt(2, 1).Count == 1);
    }

    private static void NonEmptyBagCannotBeSoldButAnEmptyOneCan()
    {
        var rng = new Random(925);
        var player = new Player("BagSellTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var trader = Trader.CreateRandom(0, 0, 1, rng);

        var bagContainer = new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Small, 4, 0);
        bagContainer.Contents.AddItem(Items.HealthPotion.Clone());
        var bagItem = new Item("Test Sellable Bag", '(', "test", ItemType.Container, weight: 0.5, goldValue: 10, container: bagContainer);
        player.Inventory.AddItem(bagItem);

        string rejection = TraderScreen.TrySell(player, trader, player.Inventory.Items.IndexOf(bagItem));
        Check("A non-empty bag cannot be sold", rejection.Contains("Empty the") && player.Inventory.Items.Contains(bagItem));

        bagContainer.Contents.RemoveItem(bagContainer.Contents.Items[0]);
        TraderScreen.TrySell(player, trader, player.Inventory.Items.IndexOf(bagItem));
        Check("An emptied bag can be sold normally", !player.Inventory.Items.Contains(bagItem) && trader.Inventory.Items.Contains(bagItem));
    }

    private static void TwoSpawnedBagsNeverShareAContentsList()
    {
        var template = ContainerCatalog.All[0];
        Check("Test setup: a catalog bag template requires a unique instance at spawn", template.RequiresUniqueInstance);

        var first = template.Clone();
        var second = template.Clone();
        first.Container.Contents.AddItem(Items.HealthPotion.Clone());

        Check("Two spawned copies of the same bag template have independent ContainerComponents", !ReferenceEquals(first.Container, second.Container));
        Check("Adding to one spawned bag's contents never affects the other's", second.Container.Contents.Items.Count == 0);
    }

    private static void OldChestDataWithoutContainerStillLoadsAndRemainsAccessible()
    {
        var oldData = new ChestData
        {
            X = 3,
            Y = 4,
            IsLocked = false,
            Difficulty = 10,
            IsOpen = true,
            Contents = new List<ItemData> { new() { Name = Items.HealthPotion.Name, IsIdentified = true } },
            Container = null // simulates a save written before this feature shipped
        };

        var chest = SaveManager.FromChestData(oldData);

        Check("An old ChestData with no Container payload still restores a usable Chest", chest != null && chest.X == 3 && chest.Y == 4);
        Check("Its previously-open state carries over from the deprecated top-level field", chest.IsOpen);
        Check("Its previous contents carry over from the deprecated top-level field, becoming accessible again", chest.Container.Contents.Items.Count == 1);
    }

    /// <summary>
    /// Same display Name is deliberately shared across the 4/6/8-slot versions of one magical
    /// tier (see ContainerCatalog's own doc comment -- slot count is shown elsewhere, not in the
    /// name) -- SaveManager.ResolveTemplate must disambiguate using the saved Container shape
    /// rather than always resolving to whichever same-named template happens to come first in
    /// Items.All.
    /// </summary>
    private static void SameNamedContainerVariantsResolveToTheCorrectSlotCapacityOnLoad()
    {
        var sixSlotTemplate = ContainerCatalog.All.First(i => i.Container.SlotCapacity == 6 && i.Container.WeightReduction == 0.25);
        var fourSlotTemplate = ContainerCatalog.All.First(i => i.Name == sixSlotTemplate.Name && i.Container.SlotCapacity == 4);
        Check("Test setup: two different catalog templates really do share the same Name", sixSlotTemplate.Name == fourSlotTemplate.Name);

        var data = new ItemData
        {
            Name = sixSlotTemplate.Name,
            IsIdentified = true,
            Container = new ContainerData
            {
                Kind = ContainerKind.PortableBag,
                SizeClass = sixSlotTemplate.Container.SizeClass,
                SlotCapacity = 6,
                WeightReduction = 0.25,
                IsOpen = false,
                Contents = new List<ItemData>()
            }
        };

        var resolved = SaveManager.ResolveItem(data);

        Check("Resolving a saved 6-slot bag returns the 6-slot template, not the same-named 4-slot one",
            resolved != null && resolved.Container.SlotCapacity == 6);
    }

    /// <summary>
    /// Regression guard for a real bug report: a save written before version 33.3.0 shortened
    /// container names ("Small Pouch of Lightening (4-slot)" -&gt; "Pouch of Lightening") lost its
    /// carried bag entirely on load, since the old Name no longer matched anything in the
    /// catalog. SaveManager.ResolveTemplate's legacy-name fallback must recover it instead.
    /// </summary>
    private static void LegacyPreRenameContainerNamesStillResolveAfterTheNameShortening()
    {
        var currentTemplate = ContainerCatalog.All.First(i =>
            i.Container.SizeClass == ContainerSizeClass.Medium && i.Container.SlotCapacity == 6 && i.Container.WeightReduction == 0.5);
        // Old naming scheme: "{Size} {Name} ({Capacity}-slot)" -- e.g. "Medium Satchel of Greater Lightening (6-slot)".
        var legacyName = $"Medium {currentTemplate.Name} (6-slot)";

        var data = new ItemData
        {
            Name = legacyName,
            IsIdentified = true,
            Container = new ContainerData
            {
                Kind = ContainerKind.PortableBag,
                SizeClass = ContainerSizeClass.Medium,
                SlotCapacity = 6,
                WeightReduction = 0.5,
                IsOpen = false,
                Contents = new List<ItemData>()
            }
        };

        var resolved = SaveManager.ResolveItem(data);

        Check("A save written under the old pre-rename container name still resolves to the correct current template instead of vanishing",
            resolved != null && resolved.Name == currentTemplate.Name && resolved.Container.SlotCapacity == 6 && resolved.Container.WeightReduction == 0.5);
    }

    private static void RemovingAnItemFromAContainerNeverAutoOffersToEquipIt()
    {
        var rng = new Random(926);
        var player = new Player("NoAutoEquipOnRemoveTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var bagContainer = new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Large, 4, 0);
        var sword = Items.ShortSword.Clone();
        sword.IsIdentified = true;
        bagContainer.Contents.AddItem(sword);

        var result = ContainerTransferService.TryRemove(player, bagContainer, sword, "Test Bag");

        Check("Removing an item from a container never auto-equips it, even into an empty compatible slot",
            result.Success && player.Equipment.Get(EquipmentSlot.PrimaryHand) == null && player.Inventory.Items.Contains(sword));
    }

    private static void ContainerContentsParticipateInComparisonRespectingLocation()
    {
        var rng = new Random(927);
        var player = new Player("ContainerCompareTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var bagContainer = new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Large, 4, 0);
        var swordInBag = Items.ShortSword.Clone();
        swordInBag.IsIdentified = true;
        bagContainer.Contents.AddItem(swordInBag);
        var bagItem = new Item("Test Compare Bag", '(', "test", ItemType.Container, weight: 1.0, container: bagContainer);
        player.Inventory.AddItem(bagItem);

        string label = ItemComparisonFormatter.OwnedItemLabel(player, swordInBag);
        Check("An item inside a carried bag is labeled with the bag's name, not just 'inventory'", label.Contains("in Test Compare Bag"));

        var mace = Items.Warhammer.Clone();
        mace.IsIdentified = true;
        Check("An item inside a bag is still comparable against an ordinary inventory item of the same category",
            ItemComparisonCompatibility.CanCompare(swordInBag, mace, player));
    }

    /// <summary>
    /// Inventory screen display split: non-container items get the compact, quick-select-able
    /// list; carried bags are excluded from it entirely (they're opened via 'O' instead) --
    /// verifies NonContainerInventoryItems itself, which both ShowInventoryTab's display and the
    /// 1-9/a-z key-dispatch site now share, so a bag can never accidentally consume (or be
    /// selected via) a quick-select slot.
    /// </summary>
    private static void NonContainerInventoryItemsExcludesCarriedBags()
    {
        var rng = new Random(928);
        var player = new Player("InventorySplitTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var sword = Items.ShortSword.Clone();
        sword.IsIdentified = true;
        var potion = Items.HealthPotion.Clone();
        var bagItem = new Item("Test Split Bag", '(', "test", ItemType.Container, weight: 1.0,
            container: new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Small, 4, 0));

        player.Inventory.AddItem(sword);
        player.Inventory.AddItem(bagItem);
        player.Inventory.AddItem(potion);

        var nonContainerItems = InventoryScreen.NonContainerInventoryItems(player);

        Check("The non-container list has exactly the two non-bag items", nonContainerItems.Count == 2);
        Check("The non-container list never includes a carried bag", !nonContainerItems.Contains(bagItem));
        Check("The non-container list preserves the original relative order of the items around the excluded bag",
            nonContainerItems[0] == sword && nonContainerItems[1] == potion);
        Check("The full inventory still contains all three items -- nothing was removed, only filtered for display",
            player.Inventory.Items.Count == 3 && player.Inventory.Items.Contains(bagItem));
    }

    /// <summary>
    /// ContainerScreen's "Inventory:" pane (and the Put flow's own selection list) must only ever
    /// show what could actually be stored in the specific container currently open -- never
    /// another container (not even for a Chest showing a bag would be wrong here, since
    /// EligibleInventoryItems is about what CAN be inserted, and a Chest legitimately accepts a
    /// bag -- see the second half of this test), and never an item too large for a bag's size
    /// class.
    /// </summary>
    private static void ContainerScreenInventoryPaneOnlyShowsItemsThatFitTheOpenContainer()
    {
        var rng = new Random(929);
        var player = new Player("EligiblePaneTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));

        var smallBag = new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Small, 4, 0);
        var verySmallItem = new Item("Test Small Ring", '=', "test", ItemType.Armor, itemSize: ItemSize.VerySmall);
        var largeItem = new Item("Test Large Armor", '[', "test", ItemType.Armor, itemSize: ItemSize.Large);
        var otherBag = new Item("Test Other Bag", '(', "test", ItemType.Container, itemSize: ItemSize.Small,
            container: new ContainerComponent(ContainerKind.PortableBag, ContainerSizeClass.Small, 4, 0));
        player.Inventory.AddItem(verySmallItem);
        player.Inventory.AddItem(largeItem);
        player.Inventory.AddItem(otherBag);

        var eligibleForSmallBag = ContainerScreen.EligibleInventoryItems(player, smallBag);
        Check("A small bag's eligible list includes a Very Small item", eligibleForSmallBag.Contains(verySmallItem));
        Check("A small bag's eligible list excludes an oversized item", !eligibleForSmallBag.Contains(largeItem));
        Check("A bag's eligible list never includes another container -- you cannot store containers inside containers",
            !eligibleForSmallBag.Contains(otherBag));

        var chest = new ContainerComponent(ContainerKind.Chest, default, ChestConfig.DefaultSlotCapacity, 0);
        var eligibleForChest = ContainerScreen.EligibleInventoryItems(player, chest);
        Check("A chest's eligible list DOES include a bag -- chests are the one exception that may hold a container",
            eligibleForChest.Contains(otherBag));
        Check("A chest's eligible list includes an ordinary large item too, since chests accept any ordinary item",
            eligibleForChest.Contains(largeItem));
    }

    /// <summary>
    /// Every screen's left margin comes from wrapping Console.Out in a MarginTextWriter for the
    /// screen's lifetime (see ScreenMargin) -- no individual Console.WriteLine call anywhere had
    /// to change. Verifies the writer prefixes every line, that nesting (InventoryScreen opening
    /// ContainerScreen opening ItemComparisonScreen, say) never doubles the margin, and that only
    /// the outermost ScreenMargin's Dispose actually un-wraps Console.Out.
    /// </summary>
    private static void MarginTextWriterPrefixesEveryLineAndScreenMarginNestsSafely()
    {
        var buffer = new StringWriter();
        var original = Console.Out;
        Console.SetOut(buffer);
        try
        {
            using (new ScreenMargin())
            {
                Console.WriteLine("first");
                using (new ScreenMargin())
                {
                    Console.WriteLine("second");
                }
                Console.WriteLine("third");
            }
            Console.WriteLine("fourth");
        }
        finally
        {
            Console.SetOut(original);
        }

        var lines = buffer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        string margin = new string(' ', ScreenMargin.LeftMargin);

        Check("Every line written while a ScreenMargin is active gets the left margin prefix",
            lines.Length >= 3 && lines[0] == margin + "first" && lines[2] == margin + "third");
        Check("A nested ScreenMargin never doubles the margin", lines[1] == margin + "second");
        Check("After the outermost ScreenMargin disposes, output is no longer margined",
            lines.Length >= 4 && lines[3] == "fourth");
    }

    /// <summary>A monster chasing the player through a 1-wide corridor with a blocking boulder directly in its path must never step onto it -- ChaseAI.TryMove routes through Level.IsBlockedForActorMovement, the same centralized check the player's own movement uses.</summary>
    private static void ChaseAiNeverStepsOntoABlockingRoomObject()
    {
        var level = BuildOpenLevel(5, 3);
        // Wall off the rows above/below the middle row so the monster can't diagonal-slide around the boulder.
        for (int x = 0; x < 5; x++)
        {
            level.Tiles[x, 0] = Tile.CreateWall();
            level.Tiles[x, 2] = Tile.CreateWall();
        }

        var rng = new Random(200);
        var player = new Player("PushBlockTarget", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng)) { X = 3, Y = 1 };
        level.Actors.Add(player);

        var monster = Monster.Restore(BaseRestoreData("boulder-chaser", attack: 0, defense: 0));
        monster.X = 1;
        monster.Y = 1;
        level.Actors.Add(monster);

        level.RoomObjects.Add(new RoomObject(2, 1, RoomObjectType.Boulder, "blocking boulder", "s", "l",
            'O', ConsoleColor.DarkGray, blocksMovement: true, blocksVision: true, blocksProjectiles: true, isMovable: false));

        monster.AI.TakeTurn(monster, level, rng);

        Check("A chasing monster never steps onto a movement-blocking RoomObject even when it sits directly in the chase path",
            monster.X == 1 && monster.Y == 1);
    }

    /// <summary>
    /// Regression check for the "stays stuck behind the object" report: unlike the sealed
    /// 1-wide-corridor case above (a genuine dead end with no possible detour), an open room gives
    /// a chasing monster room to go around a blocking RoomObject -- it must actually do so via
    /// ChaseAI's new TerrainPathfinder fallback instead of stalling in place turn after turn as if
    /// it had lost track of the player.
    /// </summary>
    private static void ChaseAiRoutesAroundABlockingRoomObjectWhenSpaceAllows()
    {
        var level = BuildOpenLevel(5, 5); // fully open -- no walls at all, so a detour is always possible
        var rng = new Random(201);
        var player = new Player("RouteAroundTarget", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng)) { X = 4, Y = 2 };
        level.Actors.Add(player);

        var monster = Monster.Restore(BaseRestoreData("boulder-router", attack: 0, defense: 0));
        monster.X = 1;
        monster.Y = 2;
        level.Actors.Add(monster);

        level.RoomObjects.Add(new RoomObject(2, 2, RoomObjectType.Boulder, "blocking boulder", "s", "l",
            'O', ConsoleColor.DarkGray, blocksMovement: true, blocksVision: true, blocksProjectiles: true, isMovable: false));

        monster.AI.TakeTurn(monster, level, rng);

        Check("With room to detour, a chasing monster steps around a blocking RoomObject instead of stalling in place",
            (monster.X, monster.Y) != (1, 2));
        Check("The detour step never lands on the boulder's own tile", (monster.X, monster.Y) != (2, 2));
    }

    /// <summary>Same fix, but for a stationary Trader (an Actor, not a RoomObject) sitting directly in the chase path -- TerrainPathfinder treats a Trader's tile as a permanent obstacle (it never moves) so the same routing kicks in.</summary>
    private static void ChaseAiRoutesAroundAStationaryTraderBlockingTheDirectPath()
    {
        var level = BuildOpenLevel(5, 5);
        var rng = new Random(202);
        var player = new Player("RouteAroundTraderTarget", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng)) { X = 4, Y = 2 };
        level.Actors.Add(player);

        var monster = Monster.Restore(BaseRestoreData("trader-router", attack: 0, defense: 0));
        monster.X = 1;
        monster.Y = 2;
        level.Actors.Add(monster);

        var trader = Trader.CreateRandom(2, 2, difficultyLevel: 1, rng);
        level.Actors.Add(trader);

        monster.AI.TakeTurn(monster, level, rng);

        Check("With room to detour, a chasing monster routes around a stationary trader instead of stalling in place",
            (monster.X, monster.Y) != (1, 2));
        Check("The detour step never lands on the trader's own tile", (monster.X, monster.Y) != (2, 2));
    }

    private static RoomObjectTrigger BuildTestTrigger(RoomObjectTriggerCondition condition, (int X, int Y)? trackedPosition, bool repeatable = false) =>
        new(condition, RoomObjectTriggerEffect.Message, repeatable, trackedPosition, message: "test trigger fired");

    private static void RoomObjectTriggerFiresOnlyUnderItsConfiguredCondition()
    {
        var level = BuildOpenLevel(5, 5);
        var rng = new Random(1);

        var movedObject = new RoomObject(1, 1, RoomObjectType.Boulder, "moved-test", "s", "l", 'O', ConsoleColor.DarkGray,
            true, false, false, true, BuildTestTrigger(RoomObjectTriggerCondition.Moved, trackedPosition: null));
        string movedResult = RoomObjectTriggerProcessor.ProcessAfterMove(movedObject, previousPosition: (0, 1), level, rng);
        Check("Condition.Moved fires on any successful push regardless of destination", movedResult != null);

        var reached = new RoomObject(5, 5, RoomObjectType.Boulder, "reached-test", "s", "l", 'O', ConsoleColor.DarkGray,
            true, false, false, true, BuildTestTrigger(RoomObjectTriggerCondition.ReachedPosition, trackedPosition: (5, 5)));
        string reachedHit = RoomObjectTriggerProcessor.ProcessAfterMove(reached, previousPosition: (4, 5), level, rng);
        Check("Condition.ReachedPosition fires when the object's new position matches TrackedPosition", reachedHit != null);

        var notReached = new RoomObject(6, 6, RoomObjectType.Boulder, "not-reached-test", "s", "l", 'O', ConsoleColor.DarkGray,
            true, false, false, true, BuildTestTrigger(RoomObjectTriggerCondition.ReachedPosition, trackedPosition: (5, 5)));
        string reachedMiss = RoomObjectTriggerProcessor.ProcessAfterMove(notReached, previousPosition: (4, 6), level, rng);
        Check("Condition.ReachedPosition does not fire when the object's new position doesn't match TrackedPosition", reachedMiss == null);

        var left = new RoomObject(4, 3, RoomObjectType.Boulder, "left-test", "s", "l", 'O', ConsoleColor.DarkGray,
            true, false, false, true, BuildTestTrigger(RoomObjectTriggerCondition.LeftPosition, trackedPosition: (3, 3)));
        string leftHit = RoomObjectTriggerProcessor.ProcessAfterMove(left, previousPosition: (3, 3), level, rng);
        Check("Condition.LeftPosition fires when the object's previous position was TrackedPosition and it moved away", leftHit != null);

        var neverThere = new RoomObject(2, 2, RoomObjectType.Boulder, "never-there-test", "s", "l", 'O', ConsoleColor.DarkGray,
            true, false, false, true, BuildTestTrigger(RoomObjectTriggerCondition.LeftPosition, trackedPosition: (3, 3)));
        string leftMiss = RoomObjectTriggerProcessor.ProcessAfterMove(neverThere, previousPosition: (1, 2), level, rng);
        Check("Condition.LeftPosition does not fire when the object's previous position wasn't TrackedPosition", leftMiss == null);
    }

    private static void OneShotRoomObjectTriggerNeverFiresTwice()
    {
        var level = BuildOpenLevel(5, 5);
        var rng = new Random(2);
        var trigger = BuildTestTrigger(RoomObjectTriggerCondition.Moved, trackedPosition: null, repeatable: false);
        var roomObject = new RoomObject(1, 1, RoomObjectType.Boulder, "one-shot-test", "s", "l", 'O', ConsoleColor.DarkGray,
            true, false, false, true, trigger);

        string first = RoomObjectTriggerProcessor.ProcessAfterMove(roomObject, previousPosition: (0, 1), level, rng);
        string second = RoomObjectTriggerProcessor.ProcessAfterMove(roomObject, previousPosition: (1, 1), level, rng);

        Check("A one-shot trigger fires the first time its condition is met", first != null);
        Check("A one-shot trigger never fires a second time even when its condition is met again", second == null);
        Check("HasActivated is set true after a one-shot trigger fires", trigger.HasActivated);
    }

    private static void RepeatableRoomObjectTriggerCanFireAgainAfterActivating()
    {
        var level = BuildOpenLevel(5, 5);
        var rng = new Random(3);
        var trigger = BuildTestTrigger(RoomObjectTriggerCondition.Moved, trackedPosition: null, repeatable: true);
        var roomObject = new RoomObject(1, 1, RoomObjectType.Boulder, "repeatable-test", "s", "l", 'O', ConsoleColor.DarkGray,
            true, false, false, true, trigger);

        string first = RoomObjectTriggerProcessor.ProcessAfterMove(roomObject, previousPosition: (0, 1), level, rng);
        string second = RoomObjectTriggerProcessor.ProcessAfterMove(roomObject, previousPosition: (1, 1), level, rng);

        Check("A repeatable trigger fires the first time its condition is met", first != null);
        Check("A repeatable trigger fires again the next time its condition is met", second != null);
    }

    private static void RevealHiddenDoorEffectCarvesTheWallAndAddsAnUnlockedDoor()
    {
        var level = BuildOpenLevel(5, 5);
        level.Tiles[3, 3] = Tile.CreateWall();
        var rng = new Random(4);

        var trigger = new RoomObjectTrigger(RoomObjectTriggerCondition.ReachedPosition, RoomObjectTriggerEffect.RevealHiddenDoor,
            trackedPosition: (2, 2), hiddenDoorPosition: (3, 3));
        var statue = new RoomObject(2, 2, RoomObjectType.Statue, "puzzle-statue-test", "s", "l", 'Q', ConsoleColor.Gray,
            true, true, true, true, trigger);

        string message = RoomObjectTriggerProcessor.ProcessAfterMove(statue, previousPosition: (1, 2), level, rng);

        Check("RevealHiddenDoor turns the target wall tile into floor", level.Tiles[3, 3].Type == TileType.Floor);
        Check("RevealHiddenDoor adds a real, unlocked Door at the target position", level.GetDoorAt(3, 3) is { IsLocked: false });
        Check("RevealHiddenDoor returns a non-null status message", message != null);
    }

    private static void SpawnCreaturesEffectSkipsAlreadyOccupiedPositions()
    {
        var level = BuildOpenLevel(6, 3);
        var occupant = Monster.Restore(BaseRestoreData("already-here", attack: 0, defense: 0));
        occupant.X = 1;
        occupant.Y = 1;
        level.Actors.Add(occupant);
        int actorsBefore = level.Actors.Count;

        var rng = new Random(5);
        var trigger = new RoomObjectTrigger(RoomObjectTriggerCondition.Moved, RoomObjectTriggerEffect.SpawnCreatures,
            spawnPositions: new List<(int X, int Y)> { (1, 1), (4, 1) }, spawnDifficultyLevel: 1);
        var statue = new RoomObject(2, 2, RoomObjectType.Statue, "spawner-test", "s", "l", 'Q', ConsoleColor.Gray,
            true, true, true, true, trigger);

        RoomObjectTriggerProcessor.ProcessAfterMove(statue, previousPosition: (1, 2), level, rng);

        Check("SpawnCreatures skips a position that's already occupied and spawns at the free one -- exactly one new actor appears",
            level.Actors.Count == actorsBefore + 1);
        Check("The new monster actually spawned at the free position, not the occupied one",
            level.GetActorAt(4, 1) != null && level.GetActorAt(4, 1) != occupant);
    }

    private static void RoomObjectDataRoundTripPreservesPositionMovabilityAndTriggerState()
    {
        var trigger = new RoomObjectTrigger(RoomObjectTriggerCondition.ReachedPosition, RoomObjectTriggerEffect.RevealHiddenDoor,
            repeatable: true, trackedPosition: (7, 8), message: "custom message", hiddenDoorPosition: (9, 10),
            spawnPositions: new List<(int X, int Y)> { (1, 2), (3, 4) }, spawnDifficultyLevel: 6) { HasActivated = true };
        var original = new RoomObject(11, 12, RoomObjectType.Statue, "round-trip-test", "s", "l", 'Q', ConsoleColor.Gray,
            true, true, true, isMovable: true, trigger);

        var data = SaveManager.ToRoomObjectData(original);
        var restored = SaveManager.FromRoomObjectData(data);

        Check("Round-tripped RoomObject preserves Type", restored.Type == RoomObjectType.Statue);
        Check("Round-tripped RoomObject preserves position", restored.X == 11 && restored.Y == 12);
        Check("Round-tripped RoomObject preserves IsMovable", restored.IsMovable);
        Check("Round-tripped Trigger preserves Condition/Effect/Repeatable/HasActivated",
            restored.Trigger.Condition == RoomObjectTriggerCondition.ReachedPosition
            && restored.Trigger.Effect == RoomObjectTriggerEffect.RevealHiddenDoor
            && restored.Trigger.Repeatable && restored.Trigger.HasActivated);
        Check("Round-tripped Trigger preserves TrackedPosition/HiddenDoorPosition/Message/SpawnDifficultyLevel",
            restored.Trigger.TrackedPosition == (7, 8) && restored.Trigger.HiddenDoorPosition == (9, 10)
            && restored.Trigger.Message == "custom message" && restored.Trigger.SpawnDifficultyLevel == 6);
        Check("Round-tripped Trigger preserves every SpawnPosition in order",
            restored.Trigger.SpawnPositions.Count == 2 && restored.Trigger.SpawnPositions[0] == (1, 2) && restored.Trigger.SpawnPositions[1] == (3, 4));
    }

    private static void RoomObjectWithNoTriggerRoundTripsWithNullTrigger()
    {
        var original = new RoomObject(3, 4, RoomObjectType.Boulder, "no-trigger-test", "s", "l", 'O', ConsoleColor.DarkGray,
            true, true, true, isMovable: true);

        var data = SaveManager.ToRoomObjectData(original);
        var restored = SaveManager.FromRoomObjectData(data);

        Check("A RoomObjectData for an object with no Trigger serializes a null Trigger", data.Trigger == null);
        Check("Restoring that data produces a RoomObject with a null Trigger", restored.Trigger == null);
    }

    /// <summary>A save file written before the Room Objects system existed simply has no "RoomObjects" JSON property at all -- LevelData's default-initialized list must deserialize to an empty one, not null, exactly like Chests/Traps/Doors already do.</summary>
    private static void OlderLevelSaveDataWithoutRoomObjectsFieldStillLoadsWithAnEmptyList()
    {
        var data = JsonSerializer.Deserialize<LevelData>("{}");
        Check("Deserializing a LevelData JSON blob with no RoomObjects key still produces a non-null, empty list",
            data.RoomObjects != null && data.RoomObjects.Count == 0);
    }

    private static readonly (int Dx, int Dy)[] TestPushDirections =
    {
        (0, -1), (0, 1), (-1, 0), (1, 0), (-1, -1), (-1, 1), (1, -1), (1, 1)
    };

    /// <summary>Runs dungeon generation across many seeds and checks every generated RoomObject sits on a genuinely valid, unambiguous tile -- never on stairs/a door/a chest/a trap/an item/an actor/another room object's tile, and never duplicating another room object's exact position.</summary>
    private static void GeneratedRoomObjectsNeverOccupyAnInvalidOrAmbiguousTile()
    {
        bool foundAnyRoomObject = false;
        bool everyRoomObjectIsValid = true;

        for (int seed = 0; seed < 200; seed++)
        {
            var rng = new Random(seed);
            var level = DungeonGenerator.Generate(floorIndex: 3, width: 60, height: 22, rng, difficultyLevel: 3);

            var seenPositions = new HashSet<(int X, int Y)>();
            foreach (var roomObject in level.RoomObjects)
            {
                foundAnyRoomObject = true;
                var pos = (roomObject.X, roomObject.Y);
                bool valid = level.Tiles[roomObject.X, roomObject.Y].Type == TileType.Floor
                    && pos != level.StairsUpPosition && pos != level.StairsDownPosition
                    && level.GetDoorAt(roomObject.X, roomObject.Y) == null
                    && level.GetChestAt(roomObject.X, roomObject.Y) == null
                    && level.GetTrapAt(roomObject.X, roomObject.Y) == null
                    && level.GetItemAt(roomObject.X, roomObject.Y) == null
                    && level.GetActorAt(roomObject.X, roomObject.Y) == null
                    && seenPositions.Add(pos);

                if (!valid)
                {
                    everyRoomObjectIsValid = false;
                }
            }
        }

        Check("200 generated seeds actually produced at least one RoomObject to check", foundAnyRoomObject);
        Check("Every generated RoomObject sits on a valid floor tile, never on stairs/a door/a chest/a trap/an item/an actor, and never overlapping another room object",
            everyRoomObjectIsValid);
    }

    /// <summary>Runs dungeon generation across many seeds and checks no generated ground item or locked chest ever lands on either stairs tile -- previously SpawnItems/SpawnChests used their own ad-hoc guards that never checked stairs, so a chest (which a non-Thief without lockpicks can never open, move, or remove) could permanently hide and block the stairs underneath it.</summary>
    private static void GeneratedGroundItemsAndChestsNeverOccupyTheStairsTiles()
    {
        bool foundAnyItem = false;
        bool foundAnyChest = false;
        bool everyItemIsOffStairs = true;
        bool everyChestIsOffStairs = true;

        for (int seed = 0; seed < 200; seed++)
        {
            var rng = new Random(seed);
            var level = DungeonGenerator.Generate(floorIndex: 3, width: 60, height: 22, rng, difficultyLevel: 3);

            foreach (var item in level.GroundItems)
            {
                foundAnyItem = true;
                var pos = (item.X, item.Y);
                if (pos == level.StairsUpPosition || pos == level.StairsDownPosition)
                {
                    everyItemIsOffStairs = false;
                }
            }

            foreach (var chest in level.Chests)
            {
                foundAnyChest = true;
                var pos = (chest.X, chest.Y);
                if (pos == level.StairsUpPosition || pos == level.StairsDownPosition)
                {
                    everyChestIsOffStairs = false;
                }
            }
        }

        Check("200 generated seeds actually produced at least one ground item to check", foundAnyItem);
        Check("200 generated seeds actually produced at least one locked chest to check", foundAnyChest);
        Check("No generated ground item ever lands on either stairs tile", everyItemIsOffStairs);
        Check("No generated locked chest ever lands on either stairs tile", everyChestIsOffStairs);
    }

    /// <summary>Regression for InventoryScreen's item-examine display: it used to Console.WriteLine a long item description directly and let the console's own auto-wrap handle it, which clipped the last few characters of the first line. The fix routes it through TextWrapper.WrapText instead -- this checks the wrapper itself never produces an over-width line, across a range of widths and description lengths.</summary>
    private static void WrapTextNeverProducesALineLongerThanTheGivenWidth()
    {
        string longDescription = "This is a deliberately long item description, the kind that easily overflows a single console line and used to get its first line clipped by a few characters before the fix.";

        bool everyLineWithinWidth = true;
        for (int width = 20; width <= 100; width += 5)
        {
            foreach (var line in TextWrapper.WrapText(longDescription, width))
            {
                if (line.Length > width)
                {
                    everyLineWithinWidth = false;
                }
            }
        }

        Check("TextWrapper.WrapText never returns a line longer than the requested width", everyLineWithinWidth);
    }

    private static void WrapTextNeverDropsAnyWordsRegardlessOfDescriptionLength()
    {
        string longDescription = "This is a deliberately long item description, the kind that easily overflows a single console line and used to get its first line clipped by a few characters before the fix.";
        string[] originalWords = longDescription.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var wrapped = TextWrapper.WrapText(longDescription, 40);
        string[] rejoinedWords = string.Join(' ', wrapped).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        Check("Wrapping a long description across multiple lines never drops or mangles a single word",
            originalWords.SequenceEqual(rejoinedWords));
    }

    private static void GeneratedMovableRoomObjectsAlwaysHaveAtLeastOneLegalPushDirection()
    {
        bool foundAnyMovableRoomObject = false;
        bool everyMovableHasALegalPushDirection = true;

        for (int seed = 0; seed < 200; seed++)
        {
            var rng = new Random(seed);
            var level = DungeonGenerator.Generate(floorIndex: 3, width: 60, height: 22, rng, difficultyLevel: 3);

            foreach (var roomObject in level.RoomObjects.Where(o => o.IsMovable))
            {
                foundAnyMovableRoomObject = true;
                bool hasLegalDirection = TestPushDirections.Any(d => !level.IsBlockedForObjectPlacement(roomObject.X + d.Dx, roomObject.Y + d.Dy));
                if (!hasLegalDirection)
                {
                    everyMovableHasALegalPushDirection = false;
                }
            }
        }

        Check("200 generated seeds actually produced at least one movable RoomObject to check", foundAnyMovableRoomObject);
        Check("Every generated movable RoomObject has at least one legal push direction -- generation never places a permanently inert 'movable' object",
            everyMovableHasALegalPushDirection);
    }

    // --- Look Here (NumPad5) and Detect Traps -----------------------------------------

    /// <summary>GameLoop's own dungeon generation is unseeded, so a controlled test that overwrites just the Tile at the player's spawn position can still be sitting on a stray trap/item/room object the generator happened to roll there -- strips all three so the scenario is genuinely as empty as the test expects.</summary>
    private static void ClearOverlaysAt(Level level, int x, int y)
    {
        level.GroundItems.RemoveAll(drop => drop.X == x && drop.Y == y);
        level.Traps.RemoveAll(t => t.X == x && t.Y == y);
        level.RoomObjects.RemoveAll(o => o.X == x && o.Y == y);
    }

    private static void LookHereReportsNothingUnusualOnAPlainOpenFloorTile()
    {
        var player = new Player("LookHereEmptyTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(220)));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        level.Tiles[player.X, player.Y] = Tile.CreateFloor(); // plain, non-dark, Normal floor type, nothing on it
        ClearOverlaysAt(level, player.X, player.Y); // dungeon generation is unseeded -- a stray trap/item/room object could otherwise land on the spawn tile

        bool consumedATurn = gameLoop.HandleLookHere(level);

        Check("Look Here never consumes a turn", !consumedATurn);
        Check("Look Here reports nothing unusual on a plain floor tile with nothing on it",
            gameLoop.StatusMessages[^1] == "Nothing unusual here.");
    }

    private static void LookHereListsNonNormalFloorTypeAndItemsOnTheCurrentTile()
    {
        var player = new Player("LookHereEffectsTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(221)));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        var tile = Tile.CreateFloor();
        tile.FloorType = FloorType.Lava;
        level.Tiles[player.X, player.Y] = tile;
        ClearOverlaysAt(level, player.X, player.Y);
        level.AddItem(player.X, player.Y, Items.HealthPotion.Clone());

        gameLoop.HandleLookHere(level);
        string message = gameLoop.StatusMessages[^1];

        Check("Look Here's room-effects section names the tile's non-Normal FloorType",
            message.Contains("Room effects (1):") && message.Contains("Lava floor"));
        Check("Look Here's items section lists the item lying on the current tile",
            message.Contains("items in room (1):") && message.Contains(Items.HealthPotion.DisplayName));
    }

    private static void LookHereShowsDarkAndWithholdsItemsWhileStandingOnAnUnilluminatedDarkTile()
    {
        var player = new Player("LookHereDarkTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(222)));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        var tile = Tile.CreateFloor();
        tile.IsDarkRoom = true;
        tile.IsIlluminated = false;
        level.Tiles[player.X, player.Y] = tile;
        ClearOverlaysAt(level, player.X, player.Y);
        level.AddItem(player.X, player.Y, Items.HealthPotion.Clone());

        gameLoop.HandleLookHere(level);
        string message = gameLoop.StatusMessages[^1];

        Check("Look Here reports Dark while standing on an unilluminated dark tile", message.Contains("Dark"));
        Check("Look Here withholds the items section while standing on an unilluminated dark tile, matching the always-on floor-item HUD line's own rule",
            !message.Contains("items in room"));
    }

    private static void DetectTrapsChanceForStatMatchesTheEstablishedStatBasedFormula()
    {
        Check("A baseline stat of 10 gives exactly the 50% base chance",
            Math.Abs(GameLoop.DetectTrapsChanceForStat(10) - 0.50) < 0.0001);
        Check("Higher stat raises the chance and lower stat lowers it, both clamped to the same 10%-95% band StatBasedIdentifyEffect already uses",
            GameLoop.DetectTrapsChanceForStat(30) > GameLoop.DetectTrapsChanceForStat(10)
            && GameLoop.DetectTrapsChanceForStat(-10) < GameLoop.DetectTrapsChanceForStat(10)
            && GameLoop.DetectTrapsChanceForStat(1000) <= 0.95
            && GameLoop.DetectTrapsChanceForStat(-1000) >= 0.10);
    }

    private static void DetectTrapsEffectOpensATimedWindowOnTheGivenStat()
    {
        var rng = new Random(210);
        var level = BuildOpenLevel(5, 5);
        var player = new Player("DetectTrapsWiring", CharacterClass.Thief, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Thief, rng));
        var context = new SpellCastingContext(player, level, turnNumber: 50, rng);

        new DetectTrapsEffect(duration: 10, PrimaryAttribute.Agility).Apply(context, new SpellCastResult());

        Check("DetectTrapsEffect opens the window until TurnNumber + Duration", player.DetectTrapsUntilTurn == 60);
        Check("DetectTrapsEffect records which stat powers the roll", player.DetectTrapsStat == PrimaryAttribute.Agility);
    }

    private static void RollDetectTrapsNeverRevealsAnythingOnceItsWindowHasExpired()
    {
        var player = new Player("DetectTrapsExpiredTester", CharacterClass.Thief, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Thief, new Random(211)));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        player.DetectTrapsUntilTurn = level.TurnNumber; // already expired -- TurnNumber >= UntilTurn
        player.DetectTrapsStat = PrimaryAttribute.Agility;
        player.Stats.Get(PrimaryAttribute.Agility).EquipmentModifier += 1000; // would be a near-certain roll if the window were still open

        var trap = new Trap(player.X + 1, player.Y, damage: 1);
        level.Traps.Add(trap);

        for (int i = 0; i < 20; i++)
        {
            gameLoop.RollDetectTraps(level);
        }

        Check("RollDetectTraps never reveals anything once its window has expired, no matter how favorable the stat",
            !trap.IsRevealed);
    }

    private static void RollDetectTrapsEventuallyRevealsANearbyTrapWhileItsWindowIsOpen()
    {
        var player = new Player("DetectTrapsActiveTester", CharacterClass.Thief, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Thief, new Random(212)));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        player.DetectTrapsUntilTurn = level.TurnNumber + 1000; // stays open for the whole test
        player.DetectTrapsStat = PrimaryAttribute.Agility;
        player.Stats.Get(PrimaryAttribute.Agility).EquipmentModifier += 1000; // pins the roll near MaxChance (0.95)

        var trap = new Trap(player.X + 1, player.Y, damage: 1);
        level.Traps.Add(trap);

        for (int i = 0; i < 60 && !trap.IsRevealed; i++)
        {
            gameLoop.RollDetectTraps(level);
        }

        Check("RollDetectTraps reveals a nearby trap within a handful of rolls once its window is open and the stat is pinned near the max chance",
            trap.IsRevealed);
    }

    /// <summary>Regression: "A hidden trap triggers! You take 1 damage." used to leak the raw damage number instead of a severity word like every other damage message in the game.</summary>
    private static void TriggeringATrapMessageUsesSeverityWordNotRawNumber()
    {
        var player = new Player("TrapMessageTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(213)));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;

        int targetX = player.X + 1, targetY = player.Y;
        level.Tiles[targetX, targetY] = Tile.CreateFloor();
        level.Actors.RemoveAll(a => a.X == targetX && a.Y == targetY && a is not Player);
        level.Traps.Add(new Trap(targetX, targetY, damage: 1));

        int messagesBefore = gameLoop.StatusMessages.Count;
        gameLoop.HandleMove(level, PlayerCommand.MoveEast);
        var newMessages = gameLoop.StatusMessages.Skip(messagesBefore).ToList();

        Check("Triggering a trap produces a status message", newMessages.Any(m => m.Contains("hidden trap triggers")));
        Check("The trap-trigger message uses a severity word, never the raw damage number",
            newMessages.Where(m => m.Contains("hidden trap triggers")).All(m => !m.Any(char.IsDigit)));
    }

    // --- Adventure Record ---------------------------------------------------------------

    private static void AwardDeathRewardsIncrementsMonstersKilledOnlyForACreditedDeath()
    {
        var rng = new Random(300);
        var player = new Player("KillCreditTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;

        var credited = Monster.CreateRandom(player.X, player.Y, 1, rng);
        credited.LastDamageOwner = player;
        credited.Health.TakeDamage(credited.Health.Max);
        level.Actors.Add(credited);
        gameLoop.AwardDeathRewards(level);
        level.RemoveDeadActors();

        Check("A player-credited death increments MonstersKilled by exactly one", player.AdventureRecord.MonstersKilled == 1);

        var uncredited = Monster.CreateRandom(player.X, player.Y, 1, rng);
        // LastDamageOwner left null -- as it would be after environmental/trap damage.
        uncredited.Health.TakeDamage(uncredited.Health.Max);
        level.Actors.Add(uncredited);
        gameLoop.AwardDeathRewards(level);

        Check("A death with no player credit (environmental/trap/another monster) never increments MonstersKilled",
            player.AdventureRecord.MonstersKilled == 1);
    }

    private static void AwardDeathRewardsCreditsAPlayerOwnedDotKillAsAKill()
    {
        var rng = new Random(301);
        var player = new Player("DotKillCreditTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;

        var monster = Monster.CreateRandom(player.X, player.Y, 1, rng);
        monster.Health.SetCurrent(1);
        monster.ActiveEffects.Add(new ActiveEffect("Poisoned", level.TurnNumber + 5)
        {
            TickDamage = 50,
            TickDamageType = DamageType.Poison,
            DamageSourceDescription = "your poisoned blade",
            Owner = player
        });
        level.Actors.Add(monster);

        EffectProcessor.Tick(level);
        gameLoop.AwardDeathRewards(level);

        Check("A player-applied DoT kill counts toward MonstersKilled even though the player never landed a direct hit",
            player.AdventureRecord.MonstersKilled == 1);
    }

    private static void AwardDeathRewardsTracksBossKillsAndTheHighestLevelBossRecord()
    {
        var rng = new Random(302);
        var player = new Player("BossRecordTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;

        Monster KillBoss(string name, int bossLevel)
        {
            var data = BaseRestoreData(name, attack: 0, defense: 0);
            data.IsBoss = true;
            data.BossName = name;
            data.BossEpithet = "Test"; // paired with EpithetFormat.The below -- DisplayName renders "{BossName} the {BossEpithet}"
            data.BossEpithetFormat = EpithetFormat.The;
            data.Level = bossLevel;
            var boss = Monster.Restore(data);
            boss.X = player.X;
            boss.Y = player.Y;
            boss.LastDamageOwner = player;
            boss.Health.TakeDamage(boss.Health.Max);
            level.Actors.Add(boss);
            gameLoop.AwardDeathRewards(level);
            level.RemoveDeadActors();
            return boss;
        }

        KillBoss("Grimlok", 8);
        Check("Killing a boss increments BossesKilled and records it as the highest-level boss",
            player.AdventureRecord.BossesKilled == 1
            && player.AdventureRecord.HighestLevelBossLevel == 8
            && player.AdventureRecord.HighestLevelBossName == "Grimlok the Test");

        KillBoss("Vandrix", 15);
        Check("A stronger boss replaces the highest-level boss record",
            player.AdventureRecord.BossesKilled == 2
            && player.AdventureRecord.HighestLevelBossLevel == 15
            && player.AdventureRecord.HighestLevelBossName == "Vandrix the Test");

        KillBoss("Pip", 3);
        Check("A weaker boss increments BossesKilled but does not replace the highest-level boss record",
            player.AdventureRecord.BossesKilled == 3
            && player.AdventureRecord.HighestLevelBossLevel == 15
            && player.AdventureRecord.HighestLevelBossName == "Vandrix the Test");
    }

    private static void CombatStatsTrackerRecordsActualDamageDealtAndExcludesOverkill()
    {
        var player = new Player("DamageDealtTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(303)));
        var monster = Monster.Restore(BaseRestoreData("overkill-target", attack: 0, defense: 0));
        monster.Health.SetCurrent(6);

        int actual = CombatStatsTracker.ApplyDamage(monster, 20, player);

        Check("ApplyDamage returns only the actual HP removed, never the full requested amount past what the target had",
            actual == 6);
        Check("Overkill damage against a monster only adds the actual HP lost (6) to DamageDealt, not the nominal 20",
            player.AdventureRecord.DamageDealt == 6);
    }

    private static void CombatStatsTrackerRecordsActualDamageTakenAndExcludesOverkill()
    {
        var player = new Player("DamageTakenTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(304)));
        player.Health.TakeDamage(player.Health.Max - 5); // leave exactly 5 HP

        int actual = CombatStatsTracker.ApplyDamage(player, 50, owner: null);

        Check("Overkill damage against the player only adds the actual HP lost (5) to DamageTaken, not the nominal 50",
            actual == 5 && player.AdventureRecord.DamageTaken == 5);
    }

    private static void CombatStatsTrackerHealingNeverReducesDamageTaken()
    {
        var player = new Player("HealNoReduceTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(305)));
        CombatStatsTracker.ApplyDamage(player, 10, owner: null);
        long damageTakenAfterHit = player.AdventureRecord.DamageTaken;

        player.Health.Heal(10);

        Check("Healing restores HP but never reduces the lifetime DamageTaken total",
            player.AdventureRecord.DamageTaken == damageTakenAfterHit);
    }

    private static void CombatStatsTrackerNeverCreditsDamageDealtWhenOwnerIsNotThePlayer()
    {
        var player = new Player("NonPlayerOwnerTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(306)));
        var attackerMonster = Monster.Restore(BaseRestoreData("dealer", attack: 0, defense: 0));
        var victimMonster = Monster.Restore(BaseRestoreData("victim", attack: 0, defense: 0));

        CombatStatsTracker.ApplyDamage(victimMonster, 5, attackerMonster); // one monster hitting another
        CombatStatsTracker.ApplyDamage(victimMonster, 5, owner: null); // environmental damage

        Check("Monster-on-monster and environmental damage never increase the player's DamageDealt",
            player.AdventureRecord.DamageDealt == 0);
    }

    private static void CollectGoldAndSpendGoldUpdateAdventureRecordOnlyOnSuccess()
    {
        var player = new Player("GoldApiTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(307)));

        player.CollectGold(100);
        Check("CollectGold increases both current Gold and GoldCollected",
            player.Gold == 100 && player.AdventureRecord.GoldCollected == 100);

        bool spent = player.SpendGold(40);
        Check("A successful SpendGold decreases Gold and increases GoldSpent by exactly the same amount",
            spent && player.Gold == 60 && player.AdventureRecord.GoldSpent == 40);

        bool overspent = player.SpendGold(1000);
        Check("A failed SpendGold (insufficient funds) changes neither Gold nor GoldSpent",
            !overspent && player.Gold == 60 && player.AdventureRecord.GoldSpent == 40);
    }

    private static void RestoreGoldNeverCountsAsGoldCollected()
    {
        var player = new Player("RestoreGoldTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(308)));

        player.RestoreGold(500);

        Check("RestoreGold (used by SaveManager on load) sets current Gold without counting it as newly collected",
            player.Gold == 500 && player.AdventureRecord.GoldCollected == 0);
    }

    private static void TraderPurchaseAndIdentificationIncreaseGoldSpent()
    {
        var rng = new Random(309);
        var player = new Player("BuySpentTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var trader = Trader.CreateRandom(1, 1, 1, rng);

        if (trader.Inventory.Items.Count == 0)
        {
            Skip("Trader purchase increases GoldSpent", "generated trader had an empty inventory this run");
        }
        else
        {
            var item = trader.Inventory.Items[0];
            int price = ItemPricingCalculator.CalculateBuyPrice(item, player);

            player.RestoreGold(0);
            TraderScreen.TryBuy(player, trader, 0);
            Check("A buy attempt without enough gold never increases GoldSpent", player.AdventureRecord.GoldSpent == 0);

            player.RestoreGold(price);
            TraderScreen.TryBuy(player, trader, 0);
            Check("A successful buy increases GoldSpent by exactly the price paid", player.AdventureRecord.GoldSpent == price);
        }

        var unidentified = Items.VenomfangDagger.Clone();
        player.Inventory.AddItem(unidentified);
        player.RestoreGold(TraderConfig.TraderIdentificationCost);
        long goldSpentBefore = player.AdventureRecord.GoldSpent;
        TraderScreen.TryIdentify(player, unidentified);

        Check("A successful identification increases GoldSpent by exactly the identification cost",
            player.AdventureRecord.GoldSpent == goldSpentBefore + TraderConfig.TraderIdentificationCost);
    }

    private static void TraderSaleIncreasesGoldCollected()
    {
        var rng = new Random(310);
        var player = new Player("SellCollectedTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var trader = Trader.CreateRandom(1, 1, 1, rng);

        var item = Items.Dagger.Clone();
        player.Inventory.AddItem(item);
        int sellPrice = ItemPricingCalculator.CalculateSellPrice(item, player);
        long goldCollectedBefore = player.AdventureRecord.GoldCollected;

        TraderScreen.TrySell(player, trader, 0);

        Check("A successful sale increases GoldCollected by exactly the sell price",
            player.AdventureRecord.GoldCollected == goldCollectedBefore + sellPrice);
    }

    private static void DeepestFloorReachedNeverDecreasesAfterAscending()
    {
        var rng = new Random(311);
        var player = new Player("DeepestFloorTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var dungeonManager = new DungeonManager(rng);

        dungeonManager.EnterFirstFloor(player);
        Check("Entering the first floor sets DeepestFloorReached to 1", player.AdventureRecord.DeepestFloorReached == 1);

        dungeonManager.Descend(player);
        dungeonManager.Descend(player);
        Check("Descending twice raises DeepestFloorReached to 3", player.AdventureRecord.DeepestFloorReached == 3);

        dungeonManager.Ascend(player);
        Check("Ascending back to floor 2 never lowers DeepestFloorReached below its lifetime maximum (3)",
            player.AdventureRecord.DeepestFloorReached == 3 && dungeonManager.CurrentFloorIndex == 2);
    }

    private static void AdventureRecordDataRoundTripPreservesEveryField()
    {
        var original = new AdventureRecord
        {
            MonstersKilled = 84,
            DamageDealt = 4281,
            DamageTaken = 1037,
            GoldCollected = 2450,
            GoldSpent = 1725,
            BossesKilled = 3,
            HighestLevelBossName = "Lormax Golden Wing",
            HighestLevelBossLevel = 12,
            DeepestFloorReached = 14,
            TimeInDungeonSeconds = 13502,
            ItemsPermanentlyLost = 7,
            ItemsMisplaced = 12,
            MisplacedItemsRecovered = 9,
            MostValuableLostItemName = "Silver Dagger",
            MostValuableLostItemValue = 185
        };

        var data = SaveManager.ToAdventureRecordData(original);
        var restored = new AdventureRecord();
        SaveManager.RestoreAdventureRecord(restored, data);

        Check("AdventureRecordData round-trips every field exactly",
            restored.MonstersKilled == original.MonstersKilled
            && restored.DamageDealt == original.DamageDealt
            && restored.DamageTaken == original.DamageTaken
            && restored.GoldCollected == original.GoldCollected
            && restored.GoldSpent == original.GoldSpent
            && restored.BossesKilled == original.BossesKilled
            && restored.HighestLevelBossName == original.HighestLevelBossName
            && restored.HighestLevelBossLevel == original.HighestLevelBossLevel
            && restored.DeepestFloorReached == original.DeepestFloorReached
            && restored.TimeInDungeonSeconds == original.TimeInDungeonSeconds
            && restored.ItemsPermanentlyLost == original.ItemsPermanentlyLost
            && restored.ItemsMisplaced == original.ItemsMisplaced
            && restored.MisplacedItemsRecovered == original.MisplacedItemsRecovered
            && restored.MostValuableLostItemName == original.MostValuableLostItemName
            && restored.MostValuableLostItemValue == original.MostValuableLostItemValue);
    }

    private static void TimeInDungeonStartsAtZeroForAFreshCharacter()
    {
        var player = new Player("FreshTimeTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(600)));
        Check("A freshly created character's AdventureRecord starts with zero time in the dungeon",
            player.AdventureRecord.TimeInDungeonSeconds == 0);
    }

    private static void FormatTimeInDungeonProducesHoursMinutesSeconds()
    {
        Check("Zero seconds formats as 00:00:00", AdventureRecordScreen.FormatTimeInDungeon(0) == "00:00:00");
        Check("3661 seconds (1h 1m 1s) formats as 01:01:01", AdventureRecordScreen.FormatTimeInDungeon(3661) == "01:01:01");
        Check("90000 seconds (25 hours) formats as 25:00:00 -- hours never wrap or truncate past a single day",
            AdventureRecordScreen.FormatTimeInDungeon(90000) == "25:00:00");
    }

    /// <summary>
    /// Genuinely sleeps for just over a second -- there's no injectable clock in GameLoop to
    /// fake this deterministically, and the whole point of this stat is real wall-clock time, so
    /// a short real sleep is the only way to verify RecordElapsedDungeonTime actually measures it
    /// rather than, say, always returning 0.
    /// </summary>
    private static void RecordElapsedDungeonTimeAccumulatesRealElapsedSeconds()
    {
        var player = new Player("ElapsedTimeTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(601)));
        var gameLoop = new GameLoop(player);

        System.Threading.Thread.Sleep(1100);
        gameLoop.RecordElapsedDungeonTime();

        Check("RecordElapsedDungeonTime folds at least the real time slept into AdventureRecord.TimeInDungeonSeconds",
            player.AdventureRecord.TimeInDungeonSeconds >= 1);
    }

    private static void RecordElapsedDungeonTimeResetsTheSessionStartSoItDoesNotDoubleCount()
    {
        var player = new Player("NoDoubleCountTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(602)));
        var gameLoop = new GameLoop(player);

        System.Threading.Thread.Sleep(1100);
        gameLoop.RecordElapsedDungeonTime();
        long afterFirstCall = player.AdventureRecord.TimeInDungeonSeconds;

        gameLoop.RecordElapsedDungeonTime(); // called again immediately, with no further real time elapsed

        Check("A second call right after the first adds essentially nothing, confirming sessionStartUtc was reset rather than re-measuring the same elapsed window",
            player.AdventureRecord.TimeInDungeonSeconds == afterFirstCall);
    }

    private static void PageCountComputesTheExpectedNumberOfPagesAtEveryBoundary()
    {
        Check("Zero items still reports one (empty) page", InventoryScreen.PageCount(0) == 1);
        Check("Exactly one full page reports one page", InventoryScreen.PageCount(InventoryScreen.SpellSkillPageSize) == 1);
        Check("One item past a full page spills into a second page", InventoryScreen.PageCount(InventoryScreen.SpellSkillPageSize + 1) == 2);
        Check("Exactly two full pages reports two pages", InventoryScreen.PageCount(InventoryScreen.SpellSkillPageSize * 2) == 2);
        Check("One item past two full pages spills into a third page", InventoryScreen.PageCount(InventoryScreen.SpellSkillPageSize * 2 + 1) == 3);
    }

    /// <summary>
    /// Drives the same GetVisibleSpells/GetVisibleSkills + Skip/Take pagination the Spells &amp;
    /// Skills tab renders with, against the real catalogs, rather than a synthetic list -- so this
    /// keeps proving the "no more than 15 per page" requirement even as spells/skills are added to
    /// the catalogs over time. Mage and Warrior are used as the classes most likely to carry the
    /// largest spell/skill lists respectively.
    /// </summary>
    private static void SpellsAndSkillsPagesNeverExceedTheConfiguredPageSizeAndCoverEveryEntryExactlyOnce()
    {
        var mage = new Player("PageTestMage", CharacterClass.Mage, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Mage, new Random(700)));
        var warrior = new Player("PageTestWarrior", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(701)));

        CheckPagesCoverEveryEntryWithinLimit(InventoryScreen.GetVisibleSpells(mage), "Spells");
        CheckPagesCoverEveryEntryWithinLimit(InventoryScreen.GetVisibleSkills(warrior), "Skills");
    }

    private static void CheckPagesCoverEveryEntryWithinLimit<T>(List<T> items, string label)
    {
        int pageCount = InventoryScreen.PageCount(items.Count);
        var seen = new HashSet<T>();
        for (int page = 0; page < pageCount; page++)
        {
            var slice = items.Skip(page * InventoryScreen.SpellSkillPageSize).Take(InventoryScreen.SpellSkillPageSize).ToList();
            Check($"{label} page {page + 1}/{pageCount} has at most {InventoryScreen.SpellSkillPageSize} entries",
                slice.Count <= InventoryScreen.SpellSkillPageSize);
            foreach (var item in slice)
            {
                seen.Add(item);
            }
        }
        Check($"Every {label} entry is covered by exactly one page", seen.Count == items.Count);
    }

    private static void OlderSaveDataWithoutAdventureRecordFieldStillLoadsWithAZeroedRecord()
    {
        var data = JsonSerializer.Deserialize<SaveData>("{}");
        Check("Deserializing a SaveData JSON blob with no AdventureRecord key leaves it null",
            data.AdventureRecord == null);

        data.ClassName = "Warrior";
        data.RaceName = "Human";
        var player = SaveManager.ToPlayer(data);

        Check("A player restored from a save with no AdventureRecord data gets a fresh, all-zero record instead of crashing",
            player.AdventureRecord.MonstersKilled == 0 && player.AdventureRecord.DeepestFloorReached == 0
            && player.AdventureRecord.HighestLevelBossName == null && player.AdventureRecord.TimeInDungeonSeconds == 0
            && player.AdventureRecord.ItemsPermanentlyLost == 0 && player.AdventureRecord.ItemsMisplaced == 0
            && player.AdventureRecord.MisplacedItemsRecovered == 0 && player.AdventureRecord.MostValuableLostItemName == null);
    }

    private static void FindMostValuableItemIncludesBothInventoryAndEquipment()
    {
        var player = new Player("MostValuableTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(312)));
        var cheapItem = Items.Dagger.Clone();
        var pricierEquippedItem = Items.LongSword.Clone();
        player.Inventory.AddItem(cheapItem);
        player.Equipment.EquipInSlot(EquipmentSlot.PrimaryHand, pricierEquippedItem);

        var (item, goldValue) = AdventureRecordScreen.FindMostValuableItem(player);

        Check("FindMostValuableItem considers equipped items, not just carried ones, and picks the pricier of the two",
            item == pricierEquippedItem && goldValue == pricierEquippedItem.GoldValue);
    }

    private static void FindMostValuableItemBreaksATieRandomly()
    {
        var player = new Player("TieBreakTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(313)));
        var itemA = Items.HealthPotion.Clone();
        var itemB = Items.HealthPotion.Clone(); // same catalog template -- identical GoldValue, distinct instances
        player.Inventory.AddItem(itemA);
        player.Inventory.AddItem(itemB);

        bool sawA = false, sawB = false;
        for (int i = 0; i < 50 && !(sawA && sawB); i++)
        {
            var (item, _) = AdventureRecordScreen.FindMostValuableItem(player);
            if (ReferenceEquals(item, itemA))
            {
                sawA = true;
            }
            else if (ReferenceEquals(item, itemB))
            {
                sawB = true;
            }
        }

        Check("A tie between equally-valuable owned items is broken randomly -- both tied items get selected across enough attempts",
            sawA && sawB);
    }

    private static void FindMostValuableItemReturnsNoneWhenThePlayerOwnsNothing()
    {
        var player = new Player("NoItemsTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(314)));

        var (item, goldValue) = AdventureRecordScreen.FindMostValuableItem(player);

        Check("A player who owns nothing gets a null item and zero gold value from FindMostValuableItem",
            item == null && goldValue == 0);
    }

    private static void AdventureRecordScreenBuildShowsNoneForBossAndItemWhenNeitherExists()
    {
        var player = new Player("EmptyRecordScreenTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(315)));

        string liveViewText = AdventureRecordScreen.Build(player, finalView: false);
        Check("The Adventure Record screen shows None for the boss, most-valuable-owned, and most-valuable-lost sections when none exist",
            liveViewText.Contains("None") && liveViewText.Split("None").Length - 1 == 3);
        Check("The non-final view ends with the 'return' footer", liveViewText.EndsWith("Press any key to return"));

        string finalViewText = AdventureRecordScreen.Build(player, finalView: true);
        Check("The final (death) view ends with the 'exit' footer instead", finalViewText.EndsWith("Press any key to exit"));
    }

    // --- Lost Items and Five-Tier Item Sizes -------------------------------------------

    private static void EveryItemSizeTierIsUsedSomewhereInTheCatalog()
    {
        var usedTiers = Items.All.Select(i => i.ItemSize).Distinct().ToHashSet();
        Check("The full item catalog uses all five ItemSize tiers -- confirms the audit actually differentiated items instead of leaving them all at one default",
            usedTiers.Count == 5);
    }

    private static void ItemSizeMappingMatchesTheDesignedMonsterLootBridge()
    {
        Check("VerySmall maps to Small monster loot eligibility", ItemSizeMapping.MinimumMonsterSizeForLoot(ItemSize.VerySmall) == Size.Small);
        Check("Small maps to Small monster loot eligibility", ItemSizeMapping.MinimumMonsterSizeForLoot(ItemSize.Small) == Size.Small);
        Check("Medium maps to Medium monster loot eligibility", ItemSizeMapping.MinimumMonsterSizeForLoot(ItemSize.Medium) == Size.Medium);
        Check("Large maps to Large monster loot eligibility", ItemSizeMapping.MinimumMonsterSizeForLoot(ItemSize.Large) == Size.Large);
        Check("Very Large maps to Large monster loot eligibility", ItemSizeMapping.MinimumMonsterSizeForLoot(ItemSize.VeryLarge) == Size.Large);
    }

    private static void ALargeMonsterCanDropAVeryLargeItemButASmallMonsterCannotDropMedium()
    {
        Check("A Large monster's size threshold permits a Very Large item (Executioner's Axe)",
            ItemSizeMapping.MinimumMonsterSizeForLoot(Items.ExecutionersAxe.ItemSize) <= Size.Large);
        Check("A Small monster's size threshold does not permit a Medium item (Mace)",
            !(ItemSizeMapping.MinimumMonsterSizeForLoot(Items.Mace.ItemSize) <= Size.Small));
        Check("A Small monster's size threshold does permit a Small item (Dagger)",
            ItemSizeMapping.MinimumMonsterSizeForLoot(Items.Dagger.ItemSize) <= Size.Small);
    }

    private static void ThrownLossChanceIsHigherOnAMissThanAHitForSmallSizes()
    {
        var verySmallItem = Items.Rock.Clone();
        var smallItem = Items.Dagger.Clone();

        Check("A thrown Very Small item's miss lost chance exceeds its hit lost chance",
            ItemLossRules.GetThrownLossChance(verySmallItem, hitActor: false) > ItemLossRules.GetThrownLossChance(verySmallItem, hitActor: true));
        Check("A thrown Small item's miss lost chance exceeds its hit lost chance",
            ItemLossRules.GetThrownLossChance(smallItem, hitActor: false) > ItemLossRules.GetThrownLossChance(smallItem, hitActor: true));
        Check("A thrown Very Small item's miss misplaced chance exceeds its hit misplaced chance",
            ItemLossRules.GetThrownMisplacedChance(verySmallItem, hitActor: false) > ItemLossRules.GetThrownMisplacedChance(verySmallItem, hitActor: true));
        Check("A thrown Small item's miss misplaced chance exceeds its hit misplaced chance",
            ItemLossRules.GetThrownMisplacedChance(smallItem, hitActor: false) > ItemLossRules.GetThrownMisplacedChance(smallItem, hitActor: true));
    }

    private static void MediumAndLargerItemsHaveNoThrownLossOrMisplacedChanceInStage1()
    {
        foreach (var size in new[] { ItemSize.Medium, ItemSize.Large, ItemSize.VeryLarge })
        {
            Check($"{size} has zero thrown lost chance on both a hit and a miss (Stage 1 -- displacement is Stage 2)",
                ItemLossConfig.ThrownLostChance(size, hitActor: true) == 0.0 && ItemLossConfig.ThrownLostChance(size, hitActor: false) == 0.0);
            Check($"{size} has zero thrown misplaced chance on both a hit and a miss (Stage 1 -- displacement is Stage 2)",
                ItemLossConfig.ThrownMisplacedChance(size, hitActor: true) == 0.0 && ItemLossConfig.ThrownMisplacedChance(size, hitActor: false) == 0.0);
        }
    }

    private static void ConcealmentDifficultyReflectsTerrainDarknessIlluminationAndStanding()
    {
        int baseDifficulty = ItemLossConfig.ConcealmentBaseDifficulty(ItemSize.VerySmall);
        Check("Base concealment difficulty matches the VerySmall table value (14)", baseDifficulty == 14);

        int Difficulty(FloorType floorType = FloorType.Normal, bool dark = false, bool thrownMiss = false, bool illuminated = false, bool standing = false) =>
            ItemLossConfig.ConcealmentDifficulty(ItemSize.VerySmall, floorType, dark, thrownMiss, isDisplacedFromOriginal: false, illuminated, standing);

        Check("A plain, lit, unoccupied tile adds no modifier", Difficulty() == baseDifficulty);
        Check("Sand adds its configured +5", Difficulty(floorType: FloorType.Sand) == baseDifficulty + 5);
        Check("Mud adds its configured +4", Difficulty(floorType: FloorType.Mud) == baseDifficulty + 4);
        Check("Swamp adds its configured +6", Difficulty(floorType: FloorType.Swamp) == baseDifficulty + 6);
        Check("Grass adds its configured +3", Difficulty(floorType: FloorType.Grass) == baseDifficulty + 3);
        Check("Water adds its configured +6", Difficulty(floorType: FloorType.Water) == baseDifficulty + 6);
        Check("Darkness adds its configured +5", Difficulty(dark: true) == baseDifficulty + 5);
        Check("A thrown miss adds its configured +3", Difficulty(thrownMiss: true) == baseDifficulty + 3);
        Check("Illumination subtracts its configured -3", Difficulty(illuminated: true) == baseDifficulty - 3);
        Check("The player standing on the tile subtracts its configured -4", Difficulty(standing: true) == baseDifficulty - 4);

        Check("Difficulty is clamped to a minimum of 1 even for a Very Large item (base 0) with every reducing modifier applied",
            ItemLossConfig.ConcealmentDifficulty(ItemSize.VeryLarge, FloorType.Normal, false, false, false, true, true) == 1);
    }

    private static void ReturningAndProtectedItemsCannotBeLost()
    {
        var returning = new Item("Test Boomerang", '/', "test", ItemType.Weapon,
            canBeThrown: true, thrownWeaponBehavior: ThrownWeaponBehavior.ReturnsToThrower, itemSize: ItemSize.VerySmall);
        Check("A ReturnsToThrower item can never be lost regardless of size", !ItemLossRules.CanBeLost(returning));

        Check("The Skeleton Key can never be lost", !ItemLossRules.CanBeLost(Items.SkeletonKey));

        var explicitlyProtected = new Item("Test Trinket", '"', "test", ItemType.Armor, itemSize: ItemSize.VerySmall, canBeLost: false);
        Check("An item explicitly marked canBeLost: false is protected even if otherwise size-eligible", !ItemLossRules.CanBeLost(explicitlyProtected));
    }

    private static void UnidentifiedButValuableItemsAreStillProtectedByTheirRealValue()
    {
        var expensiveUnidentified = new Item("Test Artifact", '"', "test", ItemType.Armor, itemSize: ItemSize.VerySmall, goldValue: 1000, isIdentified: false);
        Check("An unidentified item whose real GoldValue meets the protection threshold is still protected from loss",
            !ItemLossRules.CanBeLost(expensiveUnidentified));

        var cheapUnidentified = new Item("Test Trinket", '"', "test", ItemType.Armor, itemSize: ItemSize.VerySmall, goldValue: 10, isIdentified: false);
        Check("A cheap unidentified item of a losable size is still eligible for loss",
            ItemLossRules.CanBeLost(cheapUnidentified));
    }

    /// <summary>Forces a rolled character's Adjusted(attribute) to exactly 10 -- Attribute modifier 0 -- so a test's expected outcome doesn't depend on the specific (random-seed-driven) stat roll a Player constructor happened to produce.</summary>
    private static void NormalizeAttributeToBaseline(Player player, PrimaryAttribute attribute)
    {
        int current = player.Stats.Adjusted(attribute);
        player.Stats.Get(attribute).EquipmentModifier += 10 - current;
    }

    private static void DroppedBundleQuantityDiscountsMatchDesign()
    {
        var player = new Player("BundleTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(400)));
        NormalizeAttributeToBaseline(player, PrimaryAttribute.Agility); // Adjusted(Agility) == 10 -> the optional Agility modifier is a no-op
        var level = BuildSingleTileLevel(FloorType.Normal);
        var tile = level.Tiles[1, 1];
        var item = Items.Rock.Clone(); // VerySmall -- 0.01 dropped-lit lost chance, 0.05 dropped-lit misplaced chance

        double singleLost = ItemLossRules.GetDroppedLossChance(player, item, tile, quantity: 1);
        double bundleOfThreeLost = ItemLossRules.GetDroppedLossChance(player, item, tile, quantity: 3);
        double bundleOfFiveLost = ItemLossRules.GetDroppedLossChance(player, item, tile, quantity: 5);

        Check("A single dropped unit uses the full item-size lost chance", Math.Abs(singleLost - 0.01) < 0.0001);
        Check("A bundle of 2-4 units halves the lost chance", Math.Abs(bundleOfThreeLost - 0.005) < 0.0001);
        Check("A bundle of 5 or more units cannot be permanently lost at all", bundleOfFiveLost == 0.0);

        double singleMisplaced = ItemLossRules.GetDroppedMisplacedChance(player, item, tile, quantity: 1);
        double bundleOfThreeMisplaced = ItemLossRules.GetDroppedMisplacedChance(player, item, tile, quantity: 3);
        double bundleOfFiveMisplaced = ItemLossRules.GetDroppedMisplacedChance(player, item, tile, quantity: 5);

        Check("A single dropped unit uses the full item-size misplaced chance", Math.Abs(singleMisplaced - 0.05) < 0.0001);
        Check("A bundle of 2-4 units halves the misplaced chance too", Math.Abs(bundleOfThreeMisplaced - 0.025) < 0.0001);
        Check("A bundle of 5 or more units cannot be misplaced either", bundleOfFiveMisplaced == 0.0);
    }

    private static void ConsumeManyPeelsOffOnlyTheChosenQuantity()
    {
        var player = new Player("ConsumeManyTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(401)));
        var stack = Items.Dart.Clone();
        stack.Charges = 5;
        player.Inventory.AddItem(stack);

        ItemStacking.ConsumeMany(player.Inventory, stack, 2);
        Check("ConsumeMany reduces the stack by exactly the chosen quantity, leaving the rest untouched",
            player.Inventory.Items.Contains(stack) && stack.Charges == 3);

        ItemStacking.ConsumeMany(player.Inventory, stack, 3);
        Check("Consuming the remainder of the stack removes it from inventory entirely", !player.Inventory.Items.Contains(stack));
    }

    private static void FireAndLavaDestructionTakesPriorityOverALandingRollForBothThrownAndDropped()
    {
        var player = new Player("DestructionPriorityTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(402)));
        var lavaLevel = BuildSingleTileLevel(FloorType.Lava);
        var flammableItem = Items.ScrollOfIdentify.Clone(); // Small, Flammable by default, cheap enough to also be loss-eligible

        var thrownResult = ItemLandingResolver.ResolveThrown(player, flammableItem, ProjectileTerminationReason.Miss, lavaLevel, 1, 1, new Random(14));
        Check("A flammable item landing in lava is destroyed, never rolled for loss/misplacement, even with a would-be-losing roll",
            thrownResult.Outcome == ItemLandingOutcome.Destroyed && thrownResult.Message != null);

        var droppedResult = ItemLandingResolver.ResolveDropped(player, flammableItem, lavaLevel, 1, 1, quantity: 1, new Random(14));
        Check("The same destruction-first priority holds for a dropped item",
            droppedResult.Outcome == ItemLandingOutcome.Destroyed && droppedResult.Message != null);

        Check("Fire/lava destruction never increments ItemsPermanentlyLost -- it's a separate outcome from permanent loss",
            player.AdventureRecord.ItemsPermanentlyLost == 0);
    }

    private static void AGuaranteedSurvivingThrownItemLandsOnTheFloorExactlyOnce()
    {
        var player = new Player("SurvivingThrowTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(403)));
        var level = BuildSingleTileLevel(FloorType.Normal);
        var item = Items.LongSword.Clone(); // Large -- zero thrown lost/misplaced chance regardless of roll

        var result = ItemLandingResolver.ResolveThrown(player, item, ProjectileTerminationReason.Miss, level, 1, 1, new Random(14)); // seed 14 rolls low -- would lose/misplace a losable item

        Check("A zero-chance item always lands normally even against a favorable-for-loss roll",
            result.Outcome == ItemLandingOutcome.LandsNormally && result.Message == null);
        Check("The surviving item is present on the floor exactly once",
            level.GetItemsAt(1, 1).Count(i => ReferenceEquals(i, item)) == 1);
    }

    private static void ALowRollAgainstANonzeroChanceLosesTheItemAndNeverPlacesIt()
    {
        var player = new Player("LowRollLossTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(404)));
        var level = BuildSingleTileLevel(FloorType.Normal);
        var item = Items.Rock.Clone(); // VerySmall miss -- 0.10 lost chance

        var result = ItemLandingResolver.ResolveThrown(player, item, ProjectileTerminationReason.Miss, level, 1, 1, new Random(14)); // seed 14's first roll is ~0.0402, below the 0.10 lost threshold

        Check("A sufficiently low roll against a real nonzero chance permanently loses the item",
            result.Outcome == ItemLandingOutcome.PermanentlyLost && result.Message != null);
        Check("A lost item is never added to the level's ground items", level.GetItemsAt(1, 1).Count == 0);
        Check("Permanent loss increments ItemsPermanentlyLost by the quantity lost", player.AdventureRecord.ItemsPermanentlyLost == 1);
    }

    private static void AMidRangeRollAgainstANonzeroChanceMisplacesTheItemInsteadOfLosingIt()
    {
        var player = new Player("MisplaceRollTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(405)));
        var level = BuildSingleTileLevel(FloorType.Normal);
        var item = Items.Rock.Clone(); // VerySmall miss -- 0.10 lost, then 0.30 misplaced (0.10-0.40 band)

        int seed = FindSeedWhereFirstRollFallsIn(0.10, 0.40);
        var result = ItemLandingResolver.ResolveThrown(player, item, ProjectileTerminationReason.Miss, level, 1, 1, new Random(seed));

        Check("A mid-range roll misplaces the item rather than permanently losing it",
            result.Outcome == ItemLandingOutcome.Misplaced && result.Message != null);
        Check("A misplaced item is concealed, not visible", level.GetGroundItemsAt(1, 1).Single(g => ReferenceEquals(g.Item, item)).IsConcealed);
        Check("A misplaced item never shows up in GetItemsAt", level.GetItemsAt(1, 1).Count == 0);
        Check("Misplacement increments ItemsMisplaced by the quantity misplaced", player.AdventureRecord.ItemsMisplaced == 1);
        Check("Misplacement never increments ItemsPermanentlyLost", player.AdventureRecord.ItemsPermanentlyLost == 0);
    }

    private static void EveryLandingProducesExactlyOneOfTheFourStage1Outcomes()
    {
        var player = new Player("OneOutcomeTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(406)));
        var level = BuildSingleTileLevel(FloorType.Normal);

        bool allExactlyOne = true;
        for (int seed = 0; seed < 40; seed++)
        {
            var item = Items.Rock.Clone();
            var result = ItemLandingResolver.ResolveThrown(player, item, ProjectileTerminationReason.Miss, level, 1, 1, new Random(seed));
            level.GroundItems.RemoveAll(g => ReferenceEquals(g.Item, item)); // reset for the next iteration

            bool placedVisible = result.Outcome == ItemLandingOutcome.LandsNormally;
            bool placedConcealed = result.Outcome == ItemLandingOutcome.Misplaced;
            bool notPlaced = result.Outcome is ItemLandingOutcome.PermanentlyLost or ItemLandingOutcome.Destroyed;
            bool exactlyOneOutcomeShape = (placedVisible ? 1 : 0) + (placedConcealed ? 1 : 0) + (notPlaced ? 1 : 0) == 1;
            bool messageRuleHolds = result.Outcome == ItemLandingOutcome.LandsNormally ? result.Message == null : result.Message != null;

            if (!exactlyOneOutcomeShape || !messageRuleHolds)
            {
                allExactlyOne = false;
            }
        }

        Check("Every landing across 40 seeds produces exactly one outcome shape, with the Message-nullness rule holding for each", allExactlyOne);
    }

    private static void ProtectedItemsCanBeMisplacedButNeverPermanentlyLost()
    {
        var player = new Player("ProtectedMisplaceTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(407)));
        var level = BuildSingleTileLevel(FloorType.Normal);
        var protectedItem = new Item("Test Protected Trinket", '"', "test", ItemType.Armor, itemSize: ItemSize.VerySmall, canBeLost: false);

        bool everMisplaced = false;
        for (int seed = 0; seed < 40 && !everMisplaced; seed++)
        {
            var item = protectedItem.Clone();
            var result = ItemLandingResolver.ResolveThrown(player, item, ProjectileTerminationReason.Miss, level, 1, 1, new Random(seed));
            level.GroundItems.RemoveAll(g => ReferenceEquals(g.Item, item));

            Check($"Protected item never resolves to PermanentlyLost (seed {seed})", result.Outcome != ItemLandingOutcome.PermanentlyLost);
            if (result.Outcome == ItemLandingOutcome.Misplaced)
            {
                everMisplaced = true;
            }
        }

        Check("A protected item can still resolve to Misplaced across enough attempts", everMisplaced);
        Check("Protecting an item never touched ItemsPermanentlyLost", player.AdventureRecord.ItemsPermanentlyLost == 0);
    }

    private static void PermanentLossUpdatesTheMostValuableLostItemAndTiesKeepTheExistingRecord()
    {
        var player = new Player("MostValuableLostTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(408)));
        var level = BuildSingleTileLevel(FloorType.Normal);

        var cheapItem = new Item("Test Cheap Rock", '*', "test", ItemType.Armor, itemSize: ItemSize.VerySmall, goldValue: 5);
        var pricierItem = new Item("Test Pricier Rock", '*', "test", ItemType.Armor, itemSize: ItemSize.VerySmall, goldValue: 250);
        var tiedItem = new Item("Test Tied Rock", '*', "test", ItemType.Armor, itemSize: ItemSize.VerySmall, goldValue: 250);

        ItemLandingResolver.ResolveThrown(player, cheapItem, ProjectileTerminationReason.Miss, level, 1, 1, new Random(14)); // low roll -- guaranteed PermanentlyLost
        Check("The first permanent loss records that item as most valuable",
            player.AdventureRecord.MostValuableLostItemName == cheapItem.DisplayName && player.AdventureRecord.MostValuableLostItemValue == cheapItem.GoldValue);

        ItemLandingResolver.ResolveThrown(player, pricierItem, ProjectileTerminationReason.Miss, level, 1, 1, new Random(14));
        Check("A strictly more valuable permanent loss replaces the record",
            player.AdventureRecord.MostValuableLostItemName == pricierItem.DisplayName && player.AdventureRecord.MostValuableLostItemValue == 250);

        ItemLandingResolver.ResolveThrown(player, tiedItem, ProjectileTerminationReason.Miss, level, 1, 1, new Random(14));
        Check("A tied-value permanent loss keeps the existing record rather than replacing it",
            ReferenceEquals(player.AdventureRecord.MostValuableLostItemName, pricierItem.DisplayName));
    }

    /// <summary>Deterministically finds a seed whose Random's very first NextDouble() falls in [lowInclusive, highExclusive) -- used instead of hand-verifying a specific seed's draw by eye when a test needs to force a particular chance-roll outcome.</summary>
    private static int FindSeedWhereFirstRollFallsIn(double lowInclusive, double highExclusive)
    {
        for (int seed = 0; seed < 100000; seed++)
        {
            if (new Random(seed).NextDouble() is var roll && roll >= lowInclusive && roll < highExclusive)
            {
                return seed;
            }
        }
        throw new InvalidOperationException($"No seed found with a first roll in [{lowInclusive}, {highExclusive})");
    }

    private static void StackedQuantityDropsCountIndividualUnitsNotEntries()
    {
        var player = new Player("QuantityCountTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(409)));
        NormalizeAttributeToBaseline(player, PrimaryAttribute.Agility);
        var level = BuildSingleTileLevel(FloorType.Normal);
        level.Tiles[1, 1].IsDarkRoom = true;
        level.Tiles[1, 1].IsIlluminated = false; // maximizes the dropped-dark lost chance
        var bundle = Items.Rock.Clone();
        bundle.Charges = 4;

        // VerySmall dropped-dark lost chance (0.03), halved for a 2-4 unit bundle -> 0.015.
        double lostChance = ItemLossRules.GetDroppedLossChance(player, bundle, level.Tiles[1, 1], quantity: 4);
        int seed = FindSeedWhereFirstRollFallsIn(0.0, lostChance);

        var result = ItemLandingResolver.ResolveDropped(player, bundle, level, 1, 1, quantity: 4, new Random(seed));

        Check("Dropping a 4-unit bundle that gets permanently lost counts 4 units, not 1 entry",
            result.Outcome == ItemLandingOutcome.PermanentlyLost && player.AdventureRecord.ItemsPermanentlyLost == 4);
    }

    private static void ConcealedItemsAreExcludedFromGetItemAtGetItemsAtAndPickupChoices()
    {
        var level = BuildSingleTileLevel(FloorType.Normal);
        var concealed = level.AddConcealedItem(1, 1, Items.Rock.Clone(), concealmentDifficulty: 14, ItemLandingOrigin.Dropped);

        Check("A concealed item never comes back from GetItemAt", level.GetItemAt(1, 1) == null);
        Check("A concealed item never comes back from GetItemsAt", level.GetItemsAt(1, 1).Count == 0);
        Check("A concealed item DOES come back from GetGroundItemsAt (the unfiltered view discovery code needs)",
            level.GetGroundItemsAt(1, 1).Count == 1 && ReferenceEquals(level.GetGroundItemsAt(1, 1)[0], concealed));

        concealed.IsConcealed = false;
        Check("Once revealed, the same item now comes back from GetItemAt/GetItemsAt", level.GetItemAt(1, 1) != null && level.GetItemsAt(1, 1).Count == 1);
    }

    private static void ActiveSearchRevealsAnInRangeConcealedItemAndAlwaysConsumesATurn()
    {
        var player = new Player("SearchTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(410)));
        // Knowledge/Agility modifiers can be negative for an unlucky stat roll, and HandleSearch's
        // roll comes from GameLoop's own unseeded rng field (there's no way to inject a seed) --
        // normalizing both to a modifier of 0 means the d20 alone (1-20) always clears a
        // difficulty of 1, regardless of what CharacterStats.Roll happened to produce.
        NormalizeAttributeToBaseline(player, PrimaryAttribute.Knowledge);
        NormalizeAttributeToBaseline(player, PrimaryAttribute.Agility);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        level.GroundItems.RemoveAll(g => g.X == player.X && g.Y == player.Y);

        var groundItem = level.AddConcealedItem(player.X, player.Y, Items.Rock.Clone(), concealmentDifficulty: 1, ItemLandingOrigin.Dropped);

        bool turnConsumed = gameLoop.HandleSearch(level);

        Check("Active Search reveals an in-range, trivially-easy concealed item", !groundItem.IsConcealed);
        Check("Active Search returns true (consumes a turn) even though nothing changes about TurnCount itself here -- GameLoop's scheduler advances turns, not HandleSearch",
            turnConsumed);
    }

    private static void FailedActiveSearchLeavesTheItemConcealedButStillConsumesATurn()
    {
        var player = new Player("FailedSearchTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(411)));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        level.GroundItems.RemoveAll(g => g.X == player.X && g.Y == player.Y);

        // Impossibly high difficulty -- no roll can ever succeed against it.
        var groundItem = level.AddConcealedItem(player.X, player.Y, Items.Rock.Clone(), concealmentDifficulty: 1000, ItemLandingOrigin.Dropped);

        bool turnConsumed = gameLoop.HandleSearch(level);

        Check("A failed Active Search leaves the item concealed", groundItem.IsConcealed);
        Check("A failed Active Search still consumes a turn", turnConsumed);
    }

    private static void PassiveDiscoveryOnIlluminationIsOneShotPerItem()
    {
        var player = new Player("IlluminationDiscoveryTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(412)));
        NormalizeAttributeToBaseline(player, PrimaryAttribute.Knowledge); // same determinism reasoning as ActiveSearchRevealsAnInRangeConcealedItemAndAlwaysConsumesATurn
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;

        // A lit torch in inventory is a real active light source (LightingSystem.CollectActiveLightSources
        // doesn't require it to be equipped) -- genuinely illuminates the player's own tile through
        // RecomputeIllumination, rather than fighting that recompute by forcing Tile flags directly
        // (which RecomputeFov would just overwrite again). The player's own tile is always IsVisible
        // via FieldOfView's self-visibility guarantee.
        var torch = Items.Torch.Clone();
        torch.IsLit = true;
        player.Inventory.AddItem(torch);

        level.GroundItems.RemoveAll(g => g.X == player.X && g.Y == player.Y);
        var groundItem = level.AddConcealedItem(player.X, player.Y, Items.Rock.Clone(), concealmentDifficulty: 1, ItemLandingOrigin.Dropped);

        gameLoop.RecomputeFov();
        Check("An illuminated, trivially-easy concealed item is discovered the first time RecomputeFov sees it lit",
            !groundItem.IsConcealed);

        // Re-conceal it and call RecomputeFov again -- the one-shot flag must prevent a second roll.
        groundItem.IsConcealed = true;
        gameLoop.RecomputeFov();
        Check("A second RecomputeFov call never re-rolls the same item -- the one-shot flag already fired",
            groundItem.IsConcealed);
    }

    private static void RecoveryCounterIncrementsExactlyOnceAndRepeatedDropPickupCyclesDoNotDoubleCount()
    {
        var player = new Player("RecoveryTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(413)));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        level.GroundItems.RemoveAll(g => g.X == player.X && g.Y == player.Y);

        var item = Items.Rock.Clone();
        var groundItem = level.AddConcealedItem(player.X, player.Y, item, concealmentDifficulty: 1, ItemLandingOrigin.Dropped);
        groundItem.CountsAsMisplaced = true;
        groundItem.IsConcealed = false; // already discovered, ready to pick up

        Check("Nothing carried yet", player.Inventory.Items.Count == 0);
        // offerEquip: false -- this test is about recovery-credit bookkeeping, not the equip-offer
        // feature, and Items.Rock is now equippable (EquipmentType.AmmoThrown), which would
        // otherwise block on the interactive Y/N prompt's Console.ReadKey.
        bool pickedUp = gameLoop.HandlePickUp(level, offerEquip: false);
        Check("The discovered item is picked up", pickedUp && player.Inventory.Items.Contains(item));
        Check("Recovering a genuinely misplaced item increments MisplacedItemsRecovered exactly once", player.AdventureRecord.MisplacedItemsRecovered == 1);

        // Drop it again -- forcing LandsNormally this time (guaranteed via a size with zero
        // lost/misplaced chance) -- and pick it back up. This must NOT add a second recovery
        // credit for the same original misplacement.
        player.Inventory.RemoveItem(item);
        level.AddVisibleItem(player.X, player.Y, item, ItemLandingOrigin.Dropped); // ordinary, never-misplaced GroundItem
        gameLoop.HandlePickUp(level, offerEquip: false);
        Check("A later drop/pickup cycle of the same physical item, landing normally, does not add a second recovery credit",
            player.AdventureRecord.MisplacedItemsRecovered == 1);
    }

    private static void ConcealedGroundItemStateAndAdventureRecordItemStatsSurviveSaveLoad()
    {
        var player = new Player("PersistenceTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(414)));
        player.AdventureRecord.ItemsPermanentlyLost = 3;
        player.AdventureRecord.ItemsMisplaced = 5;
        player.AdventureRecord.MisplacedItemsRecovered = 2;
        player.AdventureRecord.MostValuableLostItemName = "Test Lost Item";
        player.AdventureRecord.MostValuableLostItemValue = 42;

        var level = BuildSingleTileLevel(FloorType.Normal);
        var groundItem = level.AddConcealedItem(1, 1, Items.Rock.Clone(), concealmentDifficulty: 11, ItemLandingOrigin.Thrown);
        groundItem.CountsAsMisplaced = true;
        groundItem.HasBeenRecovered = false;

        var levelData = SaveManager.ToLevelData(1, level);
        var restoredLevel = SaveManager.FromLevelData(levelData);
        var restoredGroundItem = restoredLevel.GetGroundItemsAt(1, 1).Single();

        Check("IsConcealed survives a save/load round trip", restoredGroundItem.IsConcealed);
        Check("ConcealmentDifficulty survives a save/load round trip", restoredGroundItem.ConcealmentDifficulty == 11);
        Check("LandingOrigin survives a save/load round trip", restoredGroundItem.LandingOrigin == ItemLandingOrigin.Thrown);
        Check("CountsAsMisplaced survives a save/load round trip", restoredGroundItem.CountsAsMisplaced);
        Check("A concealed item never appears in the restored level's visible GetItemsAt", restoredLevel.GetItemsAt(1, 1).Count == 0);

        var recordData = SaveManager.ToAdventureRecordData(player.AdventureRecord);
        var restoredRecord = new AdventureRecord();
        SaveManager.RestoreAdventureRecord(restoredRecord, recordData);
        Check("Adventure Record item statistics survive a save/load round trip",
            restoredRecord.ItemsPermanentlyLost == 3 && restoredRecord.ItemsMisplaced == 5 && restoredRecord.MisplacedItemsRecovered == 2
            && restoredRecord.MostValuableLostItemName == "Test Lost Item" && restoredRecord.MostValuableLostItemValue == 42);
    }

    private static void OldSaveGroundItemsWithoutConcealmentFieldsLoadAsVisible()
    {
        var data = new GroundItemData { X = 1, Y = 1, Item = new ItemData { Name = "Rock", IsIdentified = true } };

        Check("An older GroundItemData missing every concealment field defaults IsConcealed to false",
            !data.IsConcealed);
        Check("...and LandingOrigin defaults to Generated", data.LandingOrigin == ItemLandingOrigin.Generated);
        Check("...and CountsAsMisplaced/HasBeenRecovered default to false", !data.CountsAsMisplaced && !data.HasBeenRecovered);
    }

    private static void UnidentifiedItemLossMessagesNeverRevealTheTrueName()
    {
        var unidentified = Items.BlessedLongSword.Clone(); // catalog default: isIdentified false
        var level = BuildSingleTileLevel(FloorType.Normal);

        string thrownLossMessage = ItemLossMessages.ForThrown(unidentified, hitActor: false, level.Tiles[1, 1], new Random(1));
        Check("A permanent-loss message for an unidentified item uses DisplayName and never the true Name",
            thrownLossMessage.Contains(unidentified.DisplayName) && !thrownLossMessage.Contains(unidentified.Name));

        string misplacedMessage = ItemLossMessages.ForMisplaced(unidentified, level.Tiles[1, 1], new Random(1));
        Check("A misplaced message for an unidentified item uses DisplayName and never the true Name",
            misplacedMessage.Contains(unidentified.DisplayName) && !misplacedMessage.Contains(unidentified.Name));

        string recoveredMessage = ItemLossMessages.ForRecovered(unidentified);
        Check("A recovery message for an unidentified item uses DisplayName and never the true Name",
            recoveredMessage.Contains(unidentified.DisplayName) && !recoveredMessage.Contains(unidentified.Name));
    }

    private static void LoadingGroundItemsNeverRerollsLoss()
    {
        var data = new ItemData { Name = "Rock", IsIdentified = true };
        var first = SaveManager.ResolveItem(data);
        var second = SaveManager.ResolveItem(data);

        Check("Resolving the same saved item twice is fully deterministic -- nothing about restoring a ground item involves a random loss roll",
            first.Name == second.Name && first.ItemSize == second.ItemSize && first.CanBeLost == second.CanBeLost);
    }

    private static void OldSaveItemsResolveItemSizeFromTheCurrentCatalog()
    {
        var data = new ItemData { Name = "Dagger", IsIdentified = true };
        var resolved = SaveManager.ResolveItem(data);

        Check("An item resolved from saved data gets its ItemSize from the current catalog entry, not a stored or guessed value",
            resolved.ItemSize == Items.Dagger.ItemSize);
    }

    // --- Trader stack consolidation and partial-quantity selling -----------------------

    private static void ConsolidateStacksMergesSplitAmmoIntoOneRunningCount()
    {
        var player = new Player("ConsolidateAmmoTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(500)));
        var firstBundle = Items.Arrow.Clone();
        firstBundle.Charges = 20;
        var secondBundle = Items.Arrow.Clone();
        secondBundle.Charges = 15;
        player.Inventory.AddItem(firstBundle);
        player.Inventory.AddItem(secondBundle);

        ItemStacking.ConsolidateStacks(player.Inventory);

        Check("ConsolidateStacks merges two separately-picked-up Arrow bundles into a single entry with the combined count",
            player.Inventory.Items.Count(i => i.Name == "Arrow") == 1
            && player.Inventory.Items.First(i => i.Name == "Arrow").Charges == 35);
    }

    private static void ConsolidateStacksRespectsThePotionCapWhenMergingMultipleStacks()
    {
        var player = new Player("ConsolidatePotionCapTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(501)));
        var stackA = Items.HealthPotion.Clone();
        stackA.Charges = 4;
        var stackB = Items.HealthPotion.Clone();
        stackB.Charges = 4;
        player.Inventory.AddItem(stackA);
        player.Inventory.AddItem(stackB);

        ItemStacking.ConsolidateStacks(player.Inventory);

        var potionStacks = player.Inventory.Items.Where(i => i.Name == "Health Potion").ToList();
        Check("ConsolidateStacks tops the first potion stack up to the 5-cap and leaves the overflow as its own stack, rather than exceeding the cap",
            potionStacks.Count == 2
            && potionStacks.Sum(i => i.Charges ?? 0) == 8
            && potionStacks.All(i => (i.Charges ?? 0) <= ItemStacking.MaxPotionStackSize));
    }

    private static void ConsolidateStacksNeverMergesDifferentlyIdentifiedCopies()
    {
        var player = new Player("ConsolidateIdentifyTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(502)));
        var identified = Items.HolyArrow.Clone();
        identified.Charges = 5;
        identified.IsIdentified = true;
        var unidentified = Items.HolyArrow.Clone();
        unidentified.Charges = 5;
        unidentified.IsIdentified = false;
        player.Inventory.AddItem(identified);
        player.Inventory.AddItem(unidentified);

        ItemStacking.ConsolidateStacks(player.Inventory);

        Check("ConsolidateStacks never merges an identified copy with an unidentified copy of the same item, even though they share a Name",
            player.Inventory.Items.Count(i => i.Name == "Holy Arrow") == 2);
    }

    private static void SellingAPartialQuantityLeavesTheRestOfTheStackBehind()
    {
        var rng = new Random(503);
        var player = new Player("PartialSellTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var trader = Trader.CreateRandom(1, 1, 1, rng);
        var stack = Items.Arrow.Clone();
        stack.Charges = 20;
        player.Inventory.AddItem(stack);

        TraderScreen.TrySell(player, trader, 0, quantity: 5);

        Check("Selling 5 of a 20-arrow stack leaves exactly 15 behind in the player's inventory",
            player.Inventory.Items.Single(i => i.Name == "Arrow").Charges == 15);
        Check("The sold portion (5 arrows) is added to the trader's stock as its own entry",
            trader.Inventory.Items.Any(i => i.Name == "Arrow" && i.Charges == 5));
    }

    private static void SellingWithNoQuantityGivenSellsTheWholeStackAsBefore()
    {
        var rng = new Random(504);
        var player = new Player("FullSellTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var trader = Trader.CreateRandom(1, 1, 1, rng);
        var stack = Items.Arrow.Clone();
        stack.Charges = 20;
        player.Inventory.AddItem(stack);

        TraderScreen.TrySell(player, trader, 0);

        Check("Selling with no quantity given (the pre-existing call shape) still sells the entire stack, matching the original whole-entry behavior",
            !player.Inventory.Items.Any(i => i.Name == "Arrow"));
    }

    private static void SellingAPartialQuantityPricesOnlyThatPortion()
    {
        var rng = new Random(505);
        var player = new Player("PartialPriceTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var trader = Trader.CreateRandom(1, 1, 1, rng);
        var stack = Items.Arrow.Clone();
        stack.Charges = 20;
        player.Inventory.AddItem(stack);

        var fivePriceReference = Items.Arrow.Clone();
        fivePriceReference.Charges = 5;
        int expectedPriceForFive = ItemPricingCalculator.CalculateSellPrice(fivePriceReference, player);

        long goldBefore = player.Gold;
        TraderScreen.TrySell(player, trader, 0, quantity: 5);

        Check("Selling 5 of a stack prices only those 5, not the original 20-unit stack's own (higher) charge-scaled value",
            player.Gold == goldBefore + expectedPriceForFive);
    }

    // --- Offer to Equip Wearable Items on Pickup ----------------------------------------

    private static Player NewEquipOfferTestPlayer(string name, int seed, CharacterClass characterClass = null)
    {
        characterClass ??= CharacterClass.Warrior;
        return new Player(name, characterClass, Race.Human, CharacterStats.Roll(Race.Human, characterClass, new Random(seed)));
    }

    private static void WearableItemWithAnEmptyValidSlotIsEligibleForTheEquipOffer()
    {
        var player = NewEquipOfferTestPlayer("EligibleOfferTester", 700);
        var helmet = Items.IronHelmet.Clone();

        Check("A wearable item with an empty, otherwise-valid slot is eligible for the equip offer",
            EquipmentCompatibility.FindEligibleEmptyEquipSlot(player, helmet) == EquipmentSlot.Head);
    }

    private static void AcceptingTheEquipOfferEquipsItAndRemovesItFromInventory()
    {
        var player = NewEquipOfferTestPlayer("AcceptOfferTester", 701);
        var gameLoop = new GameLoop(player);
        var helmet = Items.IronHelmet.Clone();
        player.Inventory.AddItem(helmet); // simulates the pickup that already happened before the offer

        string message = gameLoop.ResolveEquipOffer(helmet, EquipmentSlot.Head, accepted: true);

        Check("Accepting the offer equips the item into the target slot", player.Equipment.Get(EquipmentSlot.Head) == helmet);
        Check("Accepting the offer removes the item from inventory", !player.Inventory.Items.Contains(helmet));
        Check("Accepting the offer produces a non-null confirmation message", message != null);
    }

    private static void DecliningTheEquipOfferLeavesItInInventory()
    {
        var player = NewEquipOfferTestPlayer("DeclineOfferTester", 702);
        var gameLoop = new GameLoop(player);
        var helmet = Items.IronHelmet.Clone();
        player.Inventory.AddItem(helmet);

        string message = gameLoop.ResolveEquipOffer(helmet, EquipmentSlot.Head, accepted: false);

        Check("Declining the offer leaves the slot empty", player.Equipment.Get(EquipmentSlot.Head) == null);
        Check("Declining the offer leaves the item in inventory", player.Inventory.Items.Contains(helmet));
        Check("Declining the offer produces no message (no error is displayed)", message == null);
    }

    private static void TheEquipOfferNeverChangesIsIdentified()
    {
        var player = NewEquipOfferTestPlayer("IdentificationPreservedTester", 703);
        var gameLoop = new GameLoop(player);
        var unidentifiedHelmet = Items.IronHelmet.Clone();
        unidentifiedHelmet.IsIdentified = false;
        player.Inventory.AddItem(unidentifiedHelmet);
        bool wasIdentified = unidentifiedHelmet.IsIdentified;

        gameLoop.ResolveEquipOffer(unidentifiedHelmet, EquipmentSlot.Head, accepted: true);

        Check("Equipping via the pickup offer never changes IsIdentified",
            unidentifiedHelmet.IsIdentified == wasIdentified && !unidentifiedHelmet.IsIdentified);
    }

    private static void TheEquipOfferPromptAndMessageNeverRevealAnUnidentifiedItemsTrueName()
    {
        var player = NewEquipOfferTestPlayer("NoNameLeakTester", 704);
        var gameLoop = new GameLoop(player);
        var unidentifiedHelmet = Items.IronHelmet.Clone();
        unidentifiedHelmet.IsIdentified = false;
        player.Inventory.AddItem(unidentifiedHelmet);

        string prompt = GameLoop.BuildEquipOfferPrompt(unidentifiedHelmet, EquipmentSlot.Head);
        Check("The offer prompt for an unidentified item uses DisplayName, never the true Name",
            prompt.Contains(unidentifiedHelmet.DisplayName) && !prompt.Contains(unidentifiedHelmet.Name));
        Check("The offer prompt for an unidentified item omits the definite article (\"Equip Unidentified X\", not \"Equip the Unidentified X\")",
            !prompt.StartsWith("Equip the"));

        string message = gameLoop.ResolveEquipOffer(unidentifiedHelmet, EquipmentSlot.Head, accepted: true);
        Check("The equip confirmation message for an unidentified item uses DisplayName, never the true Name",
            message.Contains(unidentifiedHelmet.DisplayName) && !message.Contains(unidentifiedHelmet.Name));
    }

    private static void TheEquipOfferUsesTheRealDisplayNameWhenIdentified()
    {
        var identifiedHelmet = Items.IronHelmet.Clone();
        identifiedHelmet.IsIdentified = true;

        string prompt = GameLoop.BuildEquipOfferPrompt(identifiedHelmet, EquipmentSlot.Head);
        Check("The offer prompt for an identified item uses its real DisplayName with the definite article",
            prompt.StartsWith($"Equip the {identifiedHelmet.DisplayName} "));
    }

    private static void NonEquipmentItemsAreIneligibleForTheEquipOffer()
    {
        var player = NewEquipOfferTestPlayer("NonEquipmentTester", 705);
        Check("A consumable (no EquipmentType) is never eligible for the equip offer",
            EquipmentCompatibility.FindEligibleEmptyEquipSlot(player, Items.HealthPotion.Clone()) == null);
    }

    private static void ClassRestrictedEquipmentIsIneligibleForTheEquipOffer()
    {
        var mage = NewEquipOfferTestPlayer("ClassRestrictedTester", 706, CharacterClass.Mage);
        // A Warrior-only heavy weapon type a Mage's class rules reject outright.
        var restricted = new Item("Test Warhammer", ')', "test", ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon,
            weaponType: WeaponType.Blunt, itemSize: ItemSize.Medium, size: Size.Medium);
        bool mageAllowsBlunt = mage.Class.AllowedWeaponTypes.Contains(WeaponType.Blunt);

        Check("Test setup: this weapon type is not allowed for the test class (otherwise the test proves nothing)", !mageAllowsBlunt);
        Check("Class-restricted equipment is never eligible for the equip offer",
            EquipmentCompatibility.FindEligibleEmptyEquipSlot(mage, restricted) == null);
    }

    private static void LevelRestrictedEquipmentIsIneligibleForTheEquipOffer()
    {
        var player = NewEquipOfferTestPlayer("LevelRestrictedTester", 707);
        var highLevelItem = new Item("Test High-Level Helm", '^', "test", ItemType.Armor, EquipmentType.Head,
            minimumLevel: player.Level + 10);

        Check("Level-restricted equipment above the player's level is never eligible for the equip offer",
            EquipmentCompatibility.FindEligibleEmptyEquipSlot(player, highLevelItem) == null);
    }

    private static void RaceRestrictedEquipmentIsIneligibleForTheEquipOffer()
    {
        var player = NewEquipOfferTestPlayer("RaceRestrictedTester", 708);
        var otherRace = Race.All.First(r => r != player.Race);
        var raceLockedItem = new Item("Test Racial Helm", '^', "test", ItemType.Armor, EquipmentType.Head, requiredRace: otherRace);

        Check("Equipment restricted to a different race is never eligible for the equip offer",
            EquipmentCompatibility.FindEligibleEmptyEquipSlot(player, raceLockedItem) == null);
    }

    private static void AnOccupiedSingleSlotPreventsTheEquipOfferAndNeverPromptsToReplace()
    {
        var player = NewEquipOfferTestPlayer("OccupiedSlotTester", 709);
        player.EquipInSlot(EquipmentSlot.Head, Items.IronHelmet.Clone());
        var secondHelmet = Items.IronHelmet.Clone();

        Check("An already-occupied single-type slot (Head) makes the item ineligible for the offer -- FindEligibleEmptyEquipSlot never falls back to a 'replace which slot?' prompt",
            EquipmentCompatibility.FindEligibleEmptyEquipSlot(player, secondHelmet) == null);
    }

    private static void ARingEquipOfferSelectsTheFirstEligibleEmptyRingSlot()
    {
        var player = NewEquipOfferTestPlayer("RingSlotOrderTester", 710);
        var ring = Items.RingOfStrength.Clone();

        Check("With both ring slots empty, the offer selects Primary Ring first",
            EquipmentCompatibility.FindEligibleEmptyEquipSlot(player, ring) == EquipmentSlot.PrimaryRing);

        player.EquipInSlot(EquipmentSlot.PrimaryRing, Items.RingOfStrength.Clone());
        Check("With Primary Ring occupied, the offer falls back to the empty Off-hand Ring",
            EquipmentCompatibility.FindEligibleEmptyEquipSlot(player, ring) == EquipmentSlot.OffHandRing);

        player.EquipInSlot(EquipmentSlot.OffHandRing, Items.RingOfStrength.Clone());
        Check("With both ring slots occupied, the offer is ineligible",
            EquipmentCompatibility.FindEligibleEmptyEquipSlot(player, ring) == null);
    }

    private static void AHandItemEquipOfferSelectsTheFirstEligibleEmptyHandSlot()
    {
        var player = NewEquipOfferTestPlayer("HandSlotOrderTester", 711, CharacterClass.Mage); // Mage allows both Dagger and Wand
        var dagger = Items.Dagger.Clone();

        Check("With both hand slots empty, the offer selects Primary Hand first",
            EquipmentCompatibility.FindEligibleEmptyEquipSlot(player, dagger) == EquipmentSlot.PrimaryHand);

        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.Dagger.Clone());
        // Two weapons without Dual Wield conflict in EITHER hand -- see
        // ASecondWeaponIsIneligibleForTheEquipOfferWithoutDualWield for that case. Use a
        // non-conflicting off-hand item (EquipmentCategory.Wand doesn't conflict with Weapon) to
        // isolate slot-order selection from the separate weapon-conflict rule.
        var wand = Items.BasicWand.Clone();
        Check("With Primary Hand occupied by a non-conflicting item, a second Hand item offers the empty Off-hand",
            EquipmentCompatibility.FindEligibleEmptyEquipSlot(player, wand) == EquipmentSlot.OffHand);
    }

    /// <summary>
    /// A launcher in RangedWeapon (e.g. from starting gear -- see
    /// StartingGearLauncherAlwaysComesWithMatchingAmmo) is a real combination a Warrior can spawn
    /// with alongside a Shield rolled as starting armor (which auto-equips into PrimaryHand, the
    /// first empty hand slot at that point, since the weapon roll went to RangedWeapon instead).
    /// A hand-slot item picked up afterward must still be offered for the empty OffHand -- neither
    /// the Sling (skipped via CanEquip's RangedWeapon/Ammunition exemption) nor the Shield (Weapon
    /// vs. Shield never conflicts) should block it.
    /// </summary>
    private static void AHandItemIsStillOfferedWithALauncherEquippedAndAShieldInPrimaryHand()
    {
        var player = NewEquipOfferTestPlayer("LauncherShieldOfferTester", 713);
        player.EquipInSlot(EquipmentSlot.RangedWeapon, Items.Sling.Clone());
        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.Shield.Clone());
        var dagger = Items.Dagger.Clone();

        Check("A Dagger is still eligible for the empty Off-hand with a Sling equipped and a Shield in Primary Hand",
            EquipmentCompatibility.FindEligibleEmptyEquipSlot(player, dagger) == EquipmentSlot.OffHand);
    }

    private static void ASecondWeaponIsIneligibleForTheEquipOfferWithoutDualWield()
    {
        var player = NewEquipOfferTestPlayer("NoDualWieldTester", 712);
        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.Dagger.Clone());
        var secondWeapon = Items.Dagger.Clone();

        Check("A second weapon is ineligible for the equip offer without Dual Wield, even with an empty Off-hand",
            EquipmentCompatibility.FindEligibleEmptyEquipSlot(player, secondWeapon) == null);
    }

    private static void ASecondWeaponIsEligibleForTheEquipOfferWithDualWield()
    {
        var player = NewEquipOfferTestPlayer("DualWieldTester", 713, CharacterClass.Thief);
        player.KnownSkills.Add(SkillCatalog.DualWield);
        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.Dagger.Clone());
        var secondWeapon = Items.Dagger.Clone();

        Check("A second weapon is eligible for the equip offer, into the empty Off-hand, once the player has Dual Wield",
            EquipmentCompatibility.FindEligibleEmptyEquipSlot(player, secondWeapon) == EquipmentSlot.OffHand);
    }

    private static void GetAllNeverOffersToEquipPickedUpItems()
    {
        var player = NewEquipOfferTestPlayer("GetAllNoOfferTester", 714);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        level.GroundItems.RemoveAll(g => g.X == player.X && g.Y == player.Y);

        var helmet = Items.IronHelmet.Clone();
        level.AddItem(player.X, player.Y, helmet);

        bool pickedUp = gameLoop.PickUpAll(level, level.GetItemsAt(player.X, player.Y));

        Check("Get All picks up a wearable item into inventory", pickedUp && player.Inventory.Items.Contains(helmet));
        Check("Get All never auto-equips a wearable item, even with an eligible empty slot", player.Equipment.Get(EquipmentSlot.Head) == null);
    }

    private static void FailedPickupNeverOffersToEquip()
    {
        var player = NewEquipOfferTestPlayer("FailedPickupNoOfferTester", 715);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        level.GroundItems.RemoveAll(g => g.X == player.X && g.Y == player.Y);

        // Absurdly overweight -- guaranteed to fail EncumbranceCalculator.CanCarry regardless of
        // the player's Strength, so PickUpItem's capacity check returns false before ever
        // reaching the equip-offer step (which would otherwise block on Console.ReadKey here).
        var tooHeavyHelmet = new Item("Test Anvil Helm", '^', "test", ItemType.Armor, EquipmentType.Head, weight: 999999);
        level.AddItem(player.X, player.Y, tooHeavyHelmet);

        bool pickedUp = gameLoop.HandlePickUp(level);

        Check("A pickup that fails on carrying capacity never equips the item", !pickedUp && player.Equipment.Get(EquipmentSlot.Head) == null);
        Check("The item that failed to be picked up remains on the floor", level.GetItemsAt(player.X, player.Y).Contains(tooHeavyHelmet));
    }

    private static void AcceptingTheEquipOfferDoesNotConsumeAnAdditionalTurn()
    {
        var player = NewEquipOfferTestPlayer("NoExtraTurnTester", 716);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        var helmet = Items.IronHelmet.Clone();
        player.Inventory.AddItem(helmet);

        int turnBefore = level.TurnNumber;
        gameLoop.ResolveEquipOffer(helmet, EquipmentSlot.Head, accepted: true);

        Check("Resolving the equip offer (accept or decline) never advances the level's own turn counter -- the pickup action that triggered the offer is the only turn consumed",
            level.TurnNumber == turnBefore);
    }

    private static void MergedStackableAmmoStillEligibleForTheEquipOffer()
    {
        // Regression: PickUpItem's stacking-merge branch used to skip the equip offer entirely,
        // on the assumption that "nothing stackable has an EquipmentType" -- true before ranged
        // weapons shipped, false now that a Shuriken (and other AmmoThrown items) are both
        // Charges-stackable AND equippable. This checks the compatibility layer the fix relies
        // on: a multi-charge stack (what merging produces) is exactly as eligible as a fresh
        // single one -- Console.ReadKey itself can't be exercised headlessly (see HandlePickUp's
        // own doc comment), so this is the deepest layer of the fix that can be tested directly.
        var player = NewEquipOfferTestPlayer("MergedStackOfferTester", 717);
        var mergedShurikenStack = Items.Shuriken.Clone();
        mergedShurikenStack.Charges = 3;

        Check("A merged, multi-charge Shuriken stack is still eligible for the equip offer, same as a fresh single one",
            EquipmentCompatibility.FindEligibleEmptyEquipSlot(player, mergedShurikenStack) == EquipmentSlot.Ammunition);
    }

    private static void SomeStackableItemsAreAlsoEquipment()
    {
        // Guards against reintroducing the exact stale assumption PickUpItem's old comment made.
        // While this holds true, the stacking-merge branch MUST still call OfferEquipAfterPickup.
        Check("At least one catalog item is both stackable (Charges-based) and equippable -- the case PickUpItem's merge branch must not skip",
            Items.All.Any(i => ItemStacking.IsStackable(i) && i.EquipmentType != EquipmentType.None));
    }

    // --- Ranged Weapons, Ammunition, and Readied Throwable Equipment ----------------------

    // --- Slot compatibility ---

    private static void BowEquipsOnlyInRangedWeaponSlot()
    {
        Check("Bow's only compatible slot is RangedWeapon",
            EquipmentCompatibility.GetCompatibleSlots(Items.Bow).SequenceEqual(new[] { EquipmentSlot.RangedWeapon }));
    }

    private static void CrossbowEquipsOnlyInRangedWeaponSlot()
    {
        Check("Crossbow's only compatible slot is RangedWeapon",
            EquipmentCompatibility.GetCompatibleSlots(Items.Crossbow).SequenceEqual(new[] { EquipmentSlot.RangedWeapon }));
    }

    private static void SlingEquipsOnlyInRangedWeaponSlot()
    {
        Check("Sling's only compatible slot is RangedWeapon",
            EquipmentCompatibility.GetCompatibleSlots(Items.Sling).SequenceEqual(new[] { EquipmentSlot.RangedWeapon }));
    }

    private static void ArrowEquipsOnlyInAmmunitionSlot()
    {
        Check("Arrow's only compatible slot is Ammunition",
            EquipmentCompatibility.GetCompatibleSlots(Items.Arrow).SequenceEqual(new[] { EquipmentSlot.Ammunition }));
    }

    private static void DedicatedThrowingWeaponsCannotEquipInAHand()
    {
        Check("Throwing Knife/Axe/Dart/Shuriken/Rock have no hand-slot compatibility (removed even though they're ItemType.Weapon)",
            !EquipmentCompatibility.GetCompatibleSlots(Items.ThrowingKnife).Contains(EquipmentSlot.PrimaryHand)
            && !EquipmentCompatibility.GetCompatibleSlots(Items.ThrowingAxe).Contains(EquipmentSlot.PrimaryHand)
            && !EquipmentCompatibility.GetCompatibleSlots(Items.Dart).Contains(EquipmentSlot.PrimaryHand)
            && !EquipmentCompatibility.GetCompatibleSlots(Items.Shuriken).Contains(EquipmentSlot.PrimaryHand)
            && !EquipmentCompatibility.GetCompatibleSlots(Items.Rock).Contains(EquipmentSlot.PrimaryHand));
    }

    private static void DedicatedThrowingWeaponsEquipInAmmunitionSlot()
    {
        Check("Throwing Knife/Axe/Dart/Shuriken/Rock are all compatible with Ammunition",
            EquipmentCompatibility.GetCompatibleSlots(Items.ThrowingKnife).SequenceEqual(new[] { EquipmentSlot.Ammunition })
            && EquipmentCompatibility.GetCompatibleSlots(Items.ThrowingAxe).SequenceEqual(new[] { EquipmentSlot.Ammunition })
            && EquipmentCompatibility.GetCompatibleSlots(Items.Dart).SequenceEqual(new[] { EquipmentSlot.Ammunition })
            && EquipmentCompatibility.GetCompatibleSlots(Items.Shuriken).SequenceEqual(new[] { EquipmentSlot.Ammunition })
            && EquipmentCompatibility.GetCompatibleSlots(Items.Rock).SequenceEqual(new[] { EquipmentSlot.Ammunition }));
    }

    private static void ShortSpearCanEquipInAHandOrAmmunitionSlotPreferringAmmunition()
    {
        var slots = EquipmentCompatibility.GetCompatibleSlots(Items.ShortSpear);
        Check("Short Spear is compatible with both a hand slot and Ammunition, preferring Ammunition first since it's carried as a stack",
            slots.Contains(EquipmentSlot.PrimaryHand) && slots.Contains(EquipmentSlot.Ammunition) && slots[0] == EquipmentSlot.Ammunition);
    }

    private static void LongSpearCanEquipInAHandOrAmmunitionSlotPreferringAHand()
    {
        var slots = EquipmentCompatibility.GetCompatibleSlots(Items.LongSpear);
        Check("Long Spear is compatible with both a hand slot and Ammunition, preferring a hand slot first since it's carried individually (not stackable)",
            slots.Contains(EquipmentSlot.PrimaryHand) && slots.Contains(EquipmentSlot.Ammunition) && slots[0] == EquipmentSlot.PrimaryHand);
        Check("Long Spear is not stackable -- the exact signal GetCompatibleSlots(Item) uses to prefer a hand slot", !ItemStacking.IsStackable(Items.LongSpear));
        Check("Short Spear IS stackable, unlike Long Spear", ItemStacking.IsStackable(Items.ShortSpear));
    }

    // --- Melee behavior ---

    private static void EquippingALauncherNeverAffectsMeleeDamageOrAttackType()
    {
        var player = NewEquipOfferTestPlayer("LauncherMeleeTester", 720);
        int baseline = player.BasePhysicalAttackPower;
        var baselineAttackType = player.AttackType;

        player.EquipInSlot(EquipmentSlot.RangedWeapon, Items.Bow.Clone());
        Check("Equipping a Bow leaves BasePhysicalAttackPower unchanged", player.BasePhysicalAttackPower == baseline);
        Check("Equipping a Bow leaves AttackType unchanged", player.AttackType == baselineAttackType);

        player.Unequip(EquipmentSlot.RangedWeapon);
        player.EquipInSlot(EquipmentSlot.RangedWeapon, Items.Crossbow.Clone());
        Check("Equipping a Crossbow leaves BasePhysicalAttackPower unchanged too", player.BasePhysicalAttackPower == baseline);
    }

    private static void LauncherWithoutAHandWeaponUsesUnarmedAttackType()
    {
        var player = NewEquipOfferTestPlayer("UnarmedLauncherTester", 721);
        player.EquipInSlot(EquipmentSlot.RangedWeapon, Items.Bow.Clone());
        Check("A Bow with no hand weapon equipped still falls back to unarmed melee (AttackType.Hit)", player.AttackType == AttackType.Hit);
    }

    private static void AHandEquippedSpearAffectsMeleeNormally()
    {
        var player = NewEquipOfferTestPlayer("HandSpearMeleeTester", 722);
        int baseline = player.BasePhysicalAttackPower;
        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.ShortSpear.Clone());

        Check("A Short Spear equipped in a hand slot raises BasePhysicalAttackPower by its own bonus",
            player.BasePhysicalAttackPower == baseline + Items.ShortSpear.PhysicalAttackBonus);
        Check("A Short Spear equipped in a hand slot sets AttackType to Pierce", player.AttackType == AttackType.Pierce);
    }

    private static void AnAmmoThrownEquippedSpearDoesNotAffectMelee()
    {
        var player = NewEquipOfferTestPlayer("AmmoSpearNoMeleeTester", 723);
        int baseline = player.BasePhysicalAttackPower;
        var baselineAttackType = player.AttackType;

        player.EquipInSlot(EquipmentSlot.Ammunition, Items.ShortSpear.Clone());

        Check("A Short Spear readied in Ammo/Thrown never raises BasePhysicalAttackPower", player.BasePhysicalAttackPower == baseline);
        Check("A Short Spear readied in Ammo/Thrown never changes AttackType", player.AttackType == baselineAttackType);
    }

    // --- Firing compatibility (via GameLoop.DecideFireProjectileAction -- the pure routing decision, no aim input needed) ---

    private static void BowFiresArrowsThroughTheLauncherPath()
    {
        var player = NewEquipOfferTestPlayer("BowFireTester", 724);
        var gameLoop = new GameLoop(player);
        player.EquipInSlot(EquipmentSlot.RangedWeapon, Items.Bow.Clone());
        player.EquipInSlot(EquipmentSlot.Ammunition, Items.Arrow.Clone());

        var decision = gameLoop.DecideFireProjectileAction(out string rejection);
        Check("Bow + readied Arrow routes through the launcher", decision == GameLoop.FireProjectileDecision.FireThroughLauncher && string.IsNullOrEmpty(rejection));
    }

    private static void BowCannotFireBolts()
    {
        var player = NewEquipOfferTestPlayer("BowBoltRejectTester", 725);
        var gameLoop = new GameLoop(player);
        player.EquipInSlot(EquipmentSlot.RangedWeapon, Items.Bow.Clone());
        player.EquipInSlot(EquipmentSlot.Ammunition, Items.Bolt.Clone());

        var decision = gameLoop.DecideFireProjectileAction(out string rejection);
        Check("Bow + readied Bolt is incompatible (Bolt isn't independently throwable either, so it's a hard rejection)",
            decision == GameLoop.FireProjectileDecision.IncompatibleWithEquippedLauncher
            && rejection == "That ammunition cannot be fired from your equipped weapon.");
    }

    private static void CrossbowFiresBoltsThroughTheLauncherPath()
    {
        var player = NewEquipOfferTestPlayer("CrossbowFireTester", 726);
        player.Level = 2; // Crossbow.MinimumLevel is 2
        var gameLoop = new GameLoop(player);
        player.EquipInSlot(EquipmentSlot.RangedWeapon, Items.Crossbow.Clone());
        player.EquipInSlot(EquipmentSlot.Ammunition, Items.Bolt.Clone());

        var decision = gameLoop.DecideFireProjectileAction(out _);
        Check("Crossbow + readied Bolt routes through the launcher", decision == GameLoop.FireProjectileDecision.FireThroughLauncher);
    }

    private static void CrossbowCannotFireArrows()
    {
        var player = NewEquipOfferTestPlayer("CrossbowArrowRejectTester", 727);
        var gameLoop = new GameLoop(player);
        player.EquipInSlot(EquipmentSlot.RangedWeapon, Items.Crossbow.Clone());
        player.EquipInSlot(EquipmentSlot.Ammunition, Items.Arrow.Clone());

        var decision = gameLoop.DecideFireProjectileAction(out string rejection);
        Check("Crossbow + readied Arrow is incompatible",
            decision == GameLoop.FireProjectileDecision.IncompatibleWithEquippedLauncher
            && rejection == "That ammunition cannot be fired from your equipped weapon.");
    }

    private static void SlingFiresSlingStonesAndRocksThroughTheLauncherPath()
    {
        var player = NewEquipOfferTestPlayer("SlingFireTester", 728);
        var gameLoop = new GameLoop(player);
        player.EquipInSlot(EquipmentSlot.RangedWeapon, Items.Sling.Clone());
        player.EquipInSlot(EquipmentSlot.Ammunition, Items.SlingStone.Clone());

        Check("Sling + readied Sling Stone routes through the launcher", gameLoop.DecideFireProjectileAction(out _) == GameLoop.FireProjectileDecision.FireThroughLauncher);

        player.Unequip(EquipmentSlot.Ammunition);
        player.EquipInSlot(EquipmentSlot.Ammunition, Items.Rock.Clone());
        Check("Sling + readied Rock ALSO routes through the launcher (Rock is dual-natured)", gameLoop.DecideFireProjectileAction(out _) == GameLoop.FireProjectileDecision.FireThroughLauncher);
    }

    private static void RockFiredFromASlingTravelsLessFarThanAProperSlingStone()
    {
        var player = NewEquipOfferTestPlayer("RockPenaltyTester", 729);
        var sling = Items.Sling.Clone();

        var rockProjectile = ProjectileFactory.ForWeaponAndAmmo(player, sling, Items.Rock.Clone(), 0, 0, (1, 0));
        var slingStoneProjectile = ProjectileFactory.ForWeaponAndAmmo(player, sling, Items.SlingStone.Clone(), 0, 0, (1, 0));

        Check("A Rock fired from a Sling has a shorter range than a proper Sling Stone fired from the same Sling",
            rockProjectile.Definition.Range < slingStoneProjectile.Definition.Range);
    }

    private static void ArrowCannotBeFiredWithoutABow()
    {
        var player = NewEquipOfferTestPlayer("ArrowNoBowTester", 730);
        var gameLoop = new GameLoop(player);
        player.EquipInSlot(EquipmentSlot.Ammunition, Items.Arrow.Clone());

        var decision = gameLoop.DecideFireProjectileAction(out string rejection);
        Check("A readied Arrow with no Ranged Weapon equipped is rejected with the exact spec-worded message",
            decision == GameLoop.FireProjectileDecision.RequiresLauncher && rejection == "Arrows require a bow.");
    }

    private static void DartCanBeThrownWithoutALauncherAndWhileABowIsEquipped()
    {
        // Dart is ThrowableCategory.Light, which every class can throw regardless of its own
        // AllowedThrowableCategories list (see DecideFireProjectileAction) -- Thief here is just
        // an arbitrary choice, not a requirement.
        var player = NewEquipOfferTestPlayer("DartNoLauncherTester", 731, CharacterClass.Thief);
        var gameLoop = new GameLoop(player);
        player.EquipInSlot(EquipmentSlot.Ammunition, Items.Dart.Clone());

        Check("A readied Dart with no launcher throws directly", gameLoop.DecideFireProjectileAction(out _) == GameLoop.FireProjectileDecision.ThrowDirectly);

        player.EquipInSlot(EquipmentSlot.RangedWeapon, Items.Bow.Clone());
        Check("A readied Dart still throws directly even with an (incompatible) Bow equipped -- the bow is simply ignored",
            gameLoop.DecideFireProjectileAction(out _) == GameLoop.FireProjectileDecision.ThrowDirectly);
    }

    /// <summary>
    /// Regression for a real bug: a Warrior spawning with a Sling (starting gear) got a Rock as
    /// its companion ammo (see StartingGearLauncherAlwaysComesWithMatchingAmmo), but throwing that
    /// Rock directly (no Sling equipped) was wrongly rejected with "Your class cannot throw Rock"
    /// -- Warrior's AllowedThrowableCategories only lists Martial, and Priest's lists nothing at
    /// all, neither including Light. But ThrowableCategory.Light's own doc comment says "no
    /// training needed... anyone can chuck one" -- that's supposed to be universal, not something
    /// each class opts into. Covers both classes whose explicit list omits Light entirely.
    /// </summary>
    private static void EveryClassCanThrowLightItemsRegardlessOfItsAllowedThrowableCategoriesList()
    {
        var warrior = NewEquipOfferTestPlayer("WarriorRockThrowTester", 734, CharacterClass.Warrior);
        var warriorLoop = new GameLoop(warrior);
        warrior.EquipInSlot(EquipmentSlot.Ammunition, Items.Rock.Clone());
        Check("A Warrior can throw a readied Rock directly, even though Rock is Light and Warrior's list only grants Martial",
            warriorLoop.DecideFireProjectileAction(out _) == GameLoop.FireProjectileDecision.ThrowDirectly);

        var priest = NewEquipOfferTestPlayer("PriestDartThrowTester", 735, CharacterClass.Priest);
        var priestLoop = new GameLoop(priest);
        priest.EquipInSlot(EquipmentSlot.Ammunition, Items.Dart.Clone());
        Check("A Priest can throw a readied Dart directly, even though Priest's list grants nothing at all",
            priestLoop.DecideFireProjectileAction(out _) == GameLoop.FireProjectileDecision.ThrowDirectly);
    }

    private static void NothingReadiedIsRejectedWithoutConsumingATurn()
    {
        var player = NewEquipOfferTestPlayer("NothingReadiedTester", 733);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        int turnBefore = level.TurnNumber;

        bool turnConsumed = gameLoop.HandleFireProjectile(level);

        Check("Firing with nothing readied is rejected and never consumes a turn", !turnConsumed && level.TurnNumber == turnBefore);
    }

    // --- Damage/accuracy/range ---

    private static void AmmunitionAndLauncherBothContributeToFiredDamage()
    {
        var player = NewEquipOfferTestPlayer("DamageContributionTester", 734);
        var bow = Items.Bow.Clone();
        var arrow = Items.Arrow.Clone();

        var projectile = ProjectileFactory.ForWeaponAndAmmo(player, bow, arrow, 0, 0, (1, 0));

        Check("A fired shot's damage includes the shooter's base, the launcher's own bonus, and the ammunition's own bonus",
            projectile.Damage == player.BasePhysicalAttackPower + bow.PhysicalAttackBonus + arrow.PhysicalAttackBonus);
    }

    private static void LauncherCannotDealProjectileDamageWithoutAmmunition()
    {
        var player = NewEquipOfferTestPlayer("NoAmmoNoDamageTester", 735);
        var gameLoop = new GameLoop(player);
        player.EquipInSlot(EquipmentSlot.RangedWeapon, Items.Bow.Clone());

        var decision = gameLoop.DecideFireProjectileAction(out string rejection);
        Check("A Bow with nothing readied at all cannot deal any projectile damage -- there's nothing to fire",
            decision == GameLoop.FireProjectileDecision.NothingReadied && rejection != null);
    }

    private static void ShortSpearHasLongerThrowingRangeThanLongSpear()
    {
        Check("Short Spear's throw range exceeds Long Spear's", Items.ShortSpear.ProjectileRange > Items.LongSpear.ProjectileRange);
    }

    private static void LongSpearDealsMoreMeleeDamageThanShortSpear()
    {
        Check("Long Spear's PhysicalAttackBonus exceeds Short Spear's", Items.LongSpear.PhysicalAttackBonus > Items.ShortSpear.PhysicalAttackBonus);
    }

    // --- Equipment timing (the observable state change InventoryScreen.Show's turn-cost detection is built on -- Show itself is a fully interactive Console.ReadKey loop that can't be driven headlessly, so this verifies the underlying signal instead) ---

    private static void EquippingOrUnequippingRangedWeaponOrAmmunitionChangesObservableSlotState()
    {
        var player = NewEquipOfferTestPlayer("SlotChangeSignalTester", 736);
        var level = new GameLoop(player).CurrentLevel;

        Item RangedSnapshot() => player.Equipment.Get(EquipmentSlot.RangedWeapon);
        Item AmmoSnapshot() => player.Equipment.Get(EquipmentSlot.Ammunition);

        var bow = Items.Bow.Clone();
        player.Inventory.AddItem(bow);
        var beforeEquip = (RangedSnapshot(), AmmoSnapshot());
        InventoryScreen.ApplyItem(player, bow, level);
        Check("Equipping a Bow changes the RangedWeapon slot's occupant -- the exact signal InventoryScreen.Show compares before/after to detect a turn-costing action",
            (RangedSnapshot(), AmmoSnapshot()) != beforeEquip);

        var beforeUnequip = (RangedSnapshot(), AmmoSnapshot());
        InventoryScreen.TryUnequip(player, EquipmentSlot.RangedWeapon);
        Check("Unequipping it changes the slot state again", (RangedSnapshot(), AmmoSnapshot()) != beforeUnequip);
    }

    private static void EquippingAnUnrelatedSlotNeverChangesRangedOrAmmunitionState()
    {
        var player = NewEquipOfferTestPlayer("UnrelatedSlotNoSignalTester", 737);
        var level = new GameLoop(player).CurrentLevel;
        var helmet = Items.IronHelmet.Clone();
        player.Inventory.AddItem(helmet);

        var before = (player.Equipment.Get(EquipmentSlot.RangedWeapon), player.Equipment.Get(EquipmentSlot.Ammunition));
        InventoryScreen.ApplyItem(player, helmet, level);
        var after = (player.Equipment.Get(EquipmentSlot.RangedWeapon), player.Equipment.Get(EquipmentSlot.Ammunition));

        Check("Equipping an unrelated slot (Head) never changes the RangedWeapon/Ammunition signal -- ordinary equips stay free", before == after);
    }

    // --- Stack handling ---

    private static void FiringOrThrowingRemovesExactlyOneUnitFromTheReadiedStack()
    {
        var player = NewEquipOfferTestPlayer("OneUnitConsumedTester", 738);
        var arrows = Items.Arrow.Clone();
        arrows.Charges = 5;
        player.EquipInSlot(EquipmentSlot.Ammunition, arrows);

        bool cleared = ItemStacking.ConsumeOneFromEquippedSlot(player, EquipmentSlot.Ammunition);

        Check("Consuming one unit from a 5-arrow readied stack leaves exactly 4, without clearing the slot",
            !cleared && player.Equipment.Get(EquipmentSlot.Ammunition).Charges == 4);
    }

    private static void TheFinalShotClearsTheAmmunitionSlot()
    {
        var player = NewEquipOfferTestPlayer("LastShotClearsSlotTester", 739);
        var arrow = Items.Arrow.Clone();
        arrow.Charges = 1;
        player.EquipInSlot(EquipmentSlot.Ammunition, arrow);

        bool cleared = ItemStacking.ConsumeOneFromEquippedSlot(player, EquipmentSlot.Ammunition);

        Check("Consuming the last unit clears the Ammunition slot entirely", cleared && player.Equipment.Get(EquipmentSlot.Ammunition) == null);
    }

    private static void ShortSpearsCanStackButLongSpearsRemainIndividual()
    {
        var a = Items.ShortSpear.Clone();
        var b = Items.ShortSpear.Clone();
        Check("Two Short Spears can stack together", ItemStacking.CanStackTogether(a, b));

        var c = Items.LongSpear.Clone();
        var d = Items.LongSpear.Clone();
        Check("Two Long Spears never stack -- IsStackable is false for Long Spear (not Charges-tracked)", !ItemStacking.CanStackTogether(c, d));
    }

    private static void DifferentlyIdentifiedAmmoStacksNeverMerge()
    {
        var identified = Items.Arrow.Clone();
        identified.IsIdentified = true;
        var unidentified = Items.Arrow.Clone();
        unidentified.IsIdentified = false;

        Check("An identified and an unidentified copy of the same ammo never stack together", !ItemStacking.CanStackTogether(identified, unidentified));
    }

    private static void ReadyingMoreOfTheSameAmmoMergesIntoTheEquippedStackInstead()
    {
        var player = NewEquipOfferTestPlayer("ReadyMergeTester", 740);
        var level = new GameLoop(player).CurrentLevel;

        var firstBundle = Items.Arrow.Clone();
        firstBundle.Charges = 10;
        player.EquipInSlot(EquipmentSlot.Ammunition, firstBundle);

        var secondBundle = Items.Arrow.Clone();
        secondBundle.Charges = 5;
        player.Inventory.AddItem(secondBundle);
        string message = InventoryScreen.ApplyItem(player, secondBundle, level);

        Check("Readying a second Arrow bundle merges into the already-equipped stack rather than replacing it",
            player.Equipment.Get(EquipmentSlot.Ammunition) == firstBundle && firstBundle.Charges == 15
            && !player.Inventory.Items.Contains(secondBundle) && message.Contains("15"));
    }

    private static void EquippingAmmunitionNeverDuplicatesOrDeletesQuantity()
    {
        var player = NewEquipOfferTestPlayer("NoQuantityLeakTester", 741);
        var level = new GameLoop(player).CurrentLevel;
        var arrows = Items.Arrow.Clone();
        arrows.Charges = 12;
        player.Inventory.AddItem(arrows);

        InventoryScreen.ApplyItem(player, arrows, level);

        Check("Equipping a 12-arrow bundle keeps its count at exactly 12 -- nothing gained or lost in the move",
            player.Equipment.Get(EquipmentSlot.Ammunition) == arrows && arrows.Charges == 12);
    }

    // --- Landing and recovery ---

    private static void FiredAmmoEntersTheLandingResolverAndCanBeRecovered()
    {
        var player = NewEquipOfferTestPlayer("FiredAmmoLandingTester", 742);
        var level = BuildSingleTileLevel(FloorType.Normal);
        var arrow = Items.Arrow.Clone();
        arrow.Charges = 1;

        // Guaranteed-survive scenario: Miss (ordinary landing, not a wall/creature hit) with a
        // roll high enough to clear both breakage and the lost/misplaced bands.
        int seed = FindSeedWhereFirstRollFallsIn(0.99, 1.0);
        var landing = ItemLandingResolver.ResolveThrown(player, arrow, ProjectileTerminationReason.Miss, level, 1, 1, new Random(seed));

        Check("A fired arrow that survives lands normally, visible on the floor -- fired ammo now enters the same pipeline a thrown item already used",
            landing.Outcome == ItemLandingOutcome.LandsNormally && level.GetItemsAt(1, 1).Contains(arrow));
    }

    private static void EnvironmentalDestructionTakesPriorityOverBreakage()
    {
        var player = NewEquipOfferTestPlayer("DestructionBeforeBreakTester", 743);
        var lavaLevel = BuildSingleTileLevel(FloorType.Lava);
        var arrow = Items.Arrow.Clone(); // Flammable by default (ItemType.Ammunition), meaningful break chance too

        var landing = ItemLandingResolver.ResolveThrown(player, arrow, ProjectileTerminationReason.HitWall, lavaLevel, 1, 1, new Random(1));

        Check("An arrow landing in lava is destroyed, never rolled for breakage even against its own configured wall-impact break chance",
            landing.Outcome == ItemLandingOutcome.Destroyed);
    }

    private static void BreakageTakesPriorityOverLossOrConcealment()
    {
        var player = NewEquipOfferTestPlayer("BreakBeforeLossTester", 744);
        var level = BuildSingleTileLevel(FloorType.Normal);
        var arrow = Items.Arrow.Clone(); // 0.35 break chance on a wall impact

        int seed = FindSeedWhereFirstRollFallsIn(0.0, Items.Arrow.BreakChanceOnWallImpact);
        var landing = ItemLandingResolver.ResolveThrown(player, arrow, ProjectileTerminationReason.HitWall, level, 1, 1, new Random(seed));

        Check("A wall-impact roll under the configured break chance breaks the arrow before any loss/concealment roll ever happens",
            landing.Outcome == ItemLandingOutcome.Broken);
    }

    private static void BrokenItemsAreNeverPlacedOnTheFloor()
    {
        var player = NewEquipOfferTestPlayer("BrokenNeverPlacedTester", 745);
        var level = BuildSingleTileLevel(FloorType.Normal);
        var arrow = Items.Arrow.Clone();

        int seed = FindSeedWhereFirstRollFallsIn(0.0, Items.Arrow.BreakChanceOnWallImpact);
        ItemLandingResolver.ResolveThrown(player, arrow, ProjectileTerminationReason.HitWall, level, 1, 1, new Random(seed));

        Check("A broken item is never added to the level's ground items", level.GetItemsAt(1, 1).Count == 0 && level.GetGroundItemsAt(1, 1).Count == 0);
    }

    private static void BreakingAnItemIncrementsItemsBroken()
    {
        var player = NewEquipOfferTestPlayer("ItemsBrokenCounterTester", 746);
        var level = BuildSingleTileLevel(FloorType.Normal);
        var arrow = Items.Arrow.Clone();

        int seed = FindSeedWhereFirstRollFallsIn(0.0, Items.Arrow.BreakChanceOnWallImpact);
        ItemLandingResolver.ResolveThrown(player, arrow, ProjectileTerminationReason.HitWall, level, 1, 1, new Random(seed));

        Check("Breaking an item increments AdventureRecord.ItemsBroken", player.AdventureRecord.ItemsBroken == 1);
        Check("Breaking an item never increments ItemsPermanentlyLost -- broken and permanently lost are distinct outcomes",
            player.AdventureRecord.ItemsPermanentlyLost == 0);
    }

    private static void RocksAndSlingStonesNeverBreakUnderTheirDefaultConfiguration()
    {
        Check("Rock has zero break chance on every impact type",
            Items.Rock.BreakChanceOnCreatureHit == 0.0 && Items.Rock.BreakChanceOnWallImpact == 0.0 && Items.Rock.BreakChanceOnOrdinaryLanding == 0.0);
        Check("Sling Stone has zero break chance on every impact type",
            Items.SlingStone.BreakChanceOnCreatureHit == 0.0 && Items.SlingStone.BreakChanceOnWallImpact == 0.0 && Items.SlingStone.BreakChanceOnOrdinaryLanding == 0.0);
    }

    private static void ReturningWeaponsRemainProtectedFromBreakage()
    {
        var returning = new Item("Test Returning Blade", '/', "test", ItemType.Weapon,
            canBeThrown: true, thrownWeaponBehavior: ThrownWeaponBehavior.ReturnsToThrower, itemSize: ItemSize.VerySmall,
            breakChanceOnCreatureHit: 1.0, breakChanceOnWallImpact: 1.0, breakChanceOnOrdinaryLanding: 1.0);

        Check("A ReturnsToThrower item can never break, even with every configured break chance at 100%", !ItemBreakageRules.CanBreak(returning));
        Check("GetBreakChance is 0 for a protected item regardless of its own configured values",
            ItemBreakageRules.GetBreakChance(returning, ProjectileTerminationReason.HitWall) == 0.0);
    }

    private static void WallImpactCreatureHitAndOrdinaryLandingUseDifferentConfiguredChances()
    {
        Check("Arrow's break chance ordering matches the design doc's general expectation: wall > creature > ordinary landing",
            Items.Arrow.BreakChanceOnWallImpact > Items.Arrow.BreakChanceOnCreatureHit
            && Items.Arrow.BreakChanceOnCreatureHit > Items.Arrow.BreakChanceOnOrdinaryLanding);
        Check("Long Spear is more likely to break than Short Spear on every impact type",
            Items.LongSpear.BreakChanceOnWallImpact > Items.ShortSpear.BreakChanceOnWallImpact
            && Items.LongSpear.BreakChanceOnCreatureHit > Items.ShortSpear.BreakChanceOnCreatureHit);
    }

    // --- Identification ---

    private static void FiringOrThrowingUnidentifiedAmmoNeverIdentifiesIt()
    {
        var unidentifiedBolt = Items.SilverBolt.Clone(); // catalog default: isIdentified false
        bool wasIdentified = unidentifiedBolt.IsIdentified;

        var projectile = ProjectileFactory.ForWeaponAndAmmo(
            NewEquipOfferTestPlayer("NoIdentifyOnFireTester", 747), Items.Crossbow, unidentifiedBolt, 0, 0, (1, 0));

        Check("Building a fired projectile from an unidentified item never changes IsIdentified",
            unidentifiedBolt.IsIdentified == wasIdentified && !unidentifiedBolt.IsIdentified);
        Check("The projectile payload is the same unidentified instance, not a re-identified copy", ReferenceEquals(projectile.PayloadItem, unidentifiedBolt));
    }

    private static void LandingAndRecoveringAnUnidentifiedItemNeverIdentifiesIt()
    {
        var player = NewEquipOfferTestPlayer("NoIdentifyOnLandTester", 748);
        var level = BuildSingleTileLevel(FloorType.Normal);
        var unidentifiedBolt = Items.SilverBolt.Clone();

        int seed = FindSeedWhereFirstRollFallsIn(0.99, 1.0);
        var landing = ItemLandingResolver.ResolveThrown(player, unidentifiedBolt, ProjectileTerminationReason.Miss, level, 1, 1, new Random(seed));

        Check("Landing an unidentified item never identifies it", landing.Outcome == ItemLandingOutcome.LandsNormally && !unidentifiedBolt.IsIdentified);
    }

    private static void BreakageMessagesNeverRevealAnUnidentifiedItemsTrueName()
    {
        var unidentifiedBolt = Items.SilverBolt.Clone();
        var (broke, message) = ItemBreakageRules.Resolve(unidentifiedBolt, ProjectileTerminationReason.HitWall, new Random(1));

        // Not every seed breaks it -- only assert the name-masking property when it actually did.
        if (broke)
        {
            Check("A breakage message for an unidentified item uses DisplayName, never the true Name",
                message.Contains(unidentifiedBolt.DisplayName) && !message.Contains(unidentifiedBolt.Name));
        }
        else
        {
            Skip("A breakage message for an unidentified item uses DisplayName, never the true Name", "seed 1 did not break the item");
        }
    }

    private static void FiringErrorMessagesNeverRevealAnUnidentifiedItemsTrueName()
    {
        var player = NewEquipOfferTestPlayer("NoNameLeakInFiringTester", 749);
        var gameLoop = new GameLoop(player);
        var unidentifiedBolt = Items.SilverBolt.Clone(); // ammo, DisplayName == "Unidentified Ammunition"
        player.EquipInSlot(EquipmentSlot.Ammunition, unidentifiedBolt);
        // No launcher equipped -- Bolt isn't independently throwable, so this rejects with the
        // "require a crossbow" message, which never touches the ammo's own name at all, but
        // confirms the routing decision itself never needed to inspect (let alone reveal) it.
        var decision = gameLoop.DecideFireProjectileAction(out string rejection);

        Check("The firing rejection path never reveals an unidentified ammo's true name",
            decision == GameLoop.FireProjectileDecision.RequiresLauncher && !rejection.Contains(unidentifiedBolt.Name));
    }

    // --- Persistence ---

    private static void EquippedItemDataRoundTripsForTheNewSlotsJustLikeAnyOther()
    {
        var arrowData = new ItemData { Name = "Arrow", Charges = 17, IsIdentified = true };
        var resolvedArrow = SaveManager.ResolveItem(arrowData);

        Check("An Arrow resolved from saved ItemData keeps its Charges -- the same generic round trip every other equipped/carried item already uses, no new persistence code needed for the new slots",
            resolvedArrow.Charges == 17 && resolvedArrow.AmmunitionType == AmmunitionType.Arrow);

        var bowData = new ItemData { Name = "Bow", IsIdentified = true };
        var resolvedBow = SaveManager.ResolveItem(bowData);
        Check("A Bow resolves normally too", resolvedBow.EquipmentType == EquipmentType.Ranged);
    }

    private static void OlderGroundItemDataWithoutTheNewFieldsStillDefaultsSafely()
    {
        // RangedWeapon/Ammunition are just two more EquipmentSlot values in the exact same
        // Dictionary<EquipmentSlot, ItemData> both Player and Monster equipment already
        // round-trips through -- an older save simply never has these two keys present, so
        // there's nothing to migrate; player.Equipment.Get returns null for them, same as any
        // slot that was never populated.
        var player = NewEquipOfferTestPlayer("OldSaveEmptySlotsTester", 750);
        Check("A freshly constructed player (standing in for an old save with no RangedWeapon/Ammunition data at all) has both new slots empty",
            player.Equipment.Get(EquipmentSlot.RangedWeapon) == null && player.Equipment.Get(EquipmentSlot.Ammunition) == null);
    }

    private static void MonsterRangedGearUpEquipsBothNewSlotsDirectly()
    {
        var rng = new Random(751);
        var monster = Monster.CreateRandom(1, 1, 1, rng);
        // Force the archer path deterministically regardless of what CreateRandom's own random
        // loot roll happened to grant, mirroring EquipGuaranteedRangedWeapon's own no-op-if-already-
        // equipped guard.
        if (monster.Equipment.Get(EquipmentSlot.RangedWeapon) == null)
        {
            monster.Equipment.EquipInSlot(EquipmentSlot.RangedWeapon, Items.Bow.Clone());
        }
        if (monster.Equipment.Get(EquipmentSlot.Ammunition) == null)
        {
            monster.Equipment.EquipInSlot(EquipmentSlot.Ammunition, Items.Arrow.Clone());
        }

        Check("A ranged monster's gear lives in the two dedicated slots, not loose in inventory",
            monster.Equipment.Get(EquipmentSlot.RangedWeapon) != null && monster.Equipment.Get(EquipmentSlot.Ammunition) != null);
        Check("A monster's RangedWeapon bonus never leaks into its own base melee attack power (same exclusion as Player)",
            monster.GetEquippedItems().Contains(monster.Equipment.Get(EquipmentSlot.RangedWeapon)));
    }

    // --- Sleep command -------------------------------------------------------------------

    private static void ResolveCommandMatchesOldReadCommandBehaviorForAFewKeys()
    {
        Check("ResolveCommand maps UpArrow to MoveNorth",
            InputHandler.ResolveCommand(new ConsoleKeyInfo(' ', ConsoleKey.UpArrow, false, false, false)) == PlayerCommand.MoveNorth);
        Check("ResolveCommand maps Spacebar to Wait",
            InputHandler.ResolveCommand(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false)) == PlayerCommand.Wait);
        Check("ResolveCommand maps 'H' to ShowHelp",
            InputHandler.ResolveCommand(new ConsoleKeyInfo('h', ConsoleKey.H, false, false, false)) == PlayerCommand.ShowHelp);
        Check("ResolveCommand maps '<' (by KeyChar) to AscendStairs regardless of the reported ConsoleKey",
            InputHandler.ResolveCommand(new ConsoleKeyInfo('<', ConsoleKey.OemComma, true, false, false)) == PlayerCommand.AscendStairs);
        Check("ResolveCommand maps an unrecognized key to None",
            InputHandler.ResolveCommand(new ConsoleKeyInfo('z', ConsoleKey.Z, false, false, false)) == PlayerCommand.None);
    }

    private static void WBindsToSleepAndItsOldReservedCommentIsGone()
    {
        Check("'W' resolves to PlayerCommand.Sleep",
            InputHandler.ResolveCommand(new ConsoleKeyInfo('w', ConsoleKey.W, false, false, false)) == PlayerCommand.Sleep);
    }

    private static void EncounterSizeCalculatorAlwaysRollsBetweenOneAndFive()
    {
        var rng = new Random(950);
        bool always = true;
        for (int difficulty = 1; difficulty <= 30; difficulty++)
        {
            for (int i = 0; i < 20; i++)
            {
                int size = EncounterSizeCalculator.RollGroupSize(difficulty, rng);
                if (size < 1 || size > 5)
                {
                    always = false;
                }
            }
        }
        Check("EncounterSizeCalculator.RollGroupSize always returns 1-5 regardless of difficulty", always);
    }

    private static void AmbushChanceDecreasesAsLuckIncreasesAndStaysClamped()
    {
        var rng = new Random(951);
        const int trials = 2000;
        int lowLuckHits = 0, highLuckHits = 0;
        for (int i = 0; i < trials; i++)
        {
            if (SleepAmbushSystem.RollAmbushChance(3, rng)) lowLuckHits++;
            if (SleepAmbushSystem.RollAmbushChance(18, rng)) highLuckHits++;
        }
        double lowRate = lowLuckHits / (double)trials;
        double highRate = highLuckHits / (double)trials;

        Check("Low Luck (3) produces a noticeably higher ambush rate than high Luck (18) over many trials", lowRate > highRate);
        Check("Ambush chance never exceeds the configured max (small statistical margin)", lowRate <= SleepConfig.AmbushMaxChance + 0.05);
        Check("Ambush chance never drops below the configured min (small statistical margin)", highRate >= SleepConfig.AmbushMinChance - 0.05);
    }

    private static void BossAmbushChanceDecreasesAsLuckIncreasesAndStaysClamped()
    {
        var rng = new Random(952);
        const int trials = 3000;
        int lowLuckHits = 0, highLuckHits = 0;
        for (int i = 0; i < trials; i++)
        {
            if (SleepAmbushSystem.RollBossChance(3, rng)) lowLuckHits++;
            if (SleepAmbushSystem.RollBossChance(18, rng)) highLuckHits++;
        }
        double lowRate = lowLuckHits / (double)trials;
        double highRate = highLuckHits / (double)trials;

        Check("Low Luck (3) produces a noticeably higher boss-ambush rate than high Luck (18) over many trials", lowRate > highRate);
        Check("Boss ambush chance never exceeds the configured max (small statistical margin)", lowRate <= SleepConfig.BossAmbushMaxChance + 0.03);
        Check("Boss ambush chance never drops below the configured min (small statistical margin)", highRate >= SleepConfig.BossAmbushMinChance - 0.03);
    }

    private static void FindAmbushPositionsNeverReturnsABlockedOrOutOfRangeTile()
    {
        var rng = new Random(953);
        var level = BuildOpenLevel(20, 20);
        var player = new Player("AmbushPosTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng)) { X = 10, Y = 10 };
        level.Actors.Add(player);

        var positions = SleepAmbushSystem.FindAmbushPositions(level, player, count: 5, rng);

        bool allValid = positions.All(p =>
            level.IsInBounds(p.X, p.Y)
            && !level.IsBlockedForActorMovement(p.X, p.Y)
            && Math.Max(Math.Abs(p.X - player.X), Math.Abs(p.Y - player.Y)) <= 6);

        Check("FindAmbushPositions only ever returns valid, in-range, unblocked tiles", allValid);
    }

    private static void FindAmbushPositionsPrefersUnseenAndNonAdjacentTiles()
    {
        var rng = new Random(954);
        var level = BuildOpenLevel(20, 20);
        var player = new Player("AmbushPrefTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng)) { X = 10, Y = 10 };
        level.Actors.Add(player);

        // Mark everything within 3 tiles as "currently visible" -- everything farther stays unseen.
        for (int x = 7; x <= 13; x++)
        {
            for (int y = 7; y <= 13; y++)
            {
                if (level.IsInBounds(x, y))
                {
                    level.Tiles[x, y].IsVisible = true;
                }
            }
        }

        var positions = SleepAmbushSystem.FindAmbushPositions(level, player, count: 1, rng);

        Check("With unseen tiles available within range, FindAmbushPositions prefers one of them over a visible tile",
            positions.Count == 1 && !level.Tiles[positions[0].X, positions[0].Y].IsVisible);
    }

    private static void FindAmbushPositionsReturnsFewerThanRequestedWhenTilesAreScarce()
    {
        var rng = new Random(955);
        var level = new Level(1, 10, 10); // every tile defaults to Wall (see Level's own ctor)
        level.Tiles[5, 5] = Tile.CreateFloor();
        level.Tiles[6, 5] = Tile.CreateFloor(); // the only other walkable tile at all
        var player = new Player("AmbushScarceTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng)) { X = 5, Y = 5 };
        level.Actors.Add(player);

        var positions = SleepAmbushSystem.FindAmbushPositions(level, player, count: 5, rng);

        Check("FindAmbushPositions never forces more placements than valid tiles actually exist", positions.Count == 1);
    }

    private static void RecoveryPhraseTierBoundariesMatchTheDesignDoc()
    {
        Check("0.0 is the lowest tier", SleepMessages.TrailingSentence(0.0) == "Your condition is scarcely improved.");
        Check("0.24 is still the lowest tier", SleepMessages.TrailingSentence(0.24) == "Your condition is scarcely improved.");
        Check("0.25 crosses into the second tier", SleepMessages.TrailingSentence(0.25) == "You feel somewhat restored.");
        Check("0.59 is still the second tier", SleepMessages.TrailingSentence(0.59) == "You feel somewhat restored.");
        Check("0.60 crosses into the third tier", SleepMessages.TrailingSentence(0.60) == "You feel greatly restored, though not yet at your best.");
        Check("0.89 is still the third tier", SleepMessages.TrailingSentence(0.89) == "You feel greatly restored, though not yet at your best.");
        Check("0.90 crosses into the top tier", SleepMessages.TrailingSentence(0.90) == "You feel almost fully restored.");
        Check("1.0 is still the top tier", SleepMessages.TrailingSentence(1.0) == "You feel almost fully restored.");
    }

    private static void SleepIsRejectedWhileAHarmfulEffectIsActive()
    {
        var player = NewEquipOfferTestPlayer("SleepRejectDotTester", 956);
        player.Health.TakeDamage(10); // not full, so the "already full" rejection wouldn't otherwise fire
        player.ActiveEffects.Add(new ActiveEffect("Poisoned", 999) { TickDamage = 3, TickDamageType = DamageType.Poison });
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        // GameLoop's own internal rng is unseeded, so the generated dungeon (and the FloorType
        // under the player's spawn tile) differs every run -- pin it to a safe type so this test
        // exercises only the harmful-effect rejection, not an occasional random Lava/Fire spawn.
        level.Tiles[player.X, player.Y].FloorType = FloorType.Normal;
        int turnBefore = level.TurnNumber;

        bool started = gameLoop.HandleStartSleeping(level);

        Check("Sleep is rejected while a harmful DoT is active", !started && !gameLoop.IsSleeping);
        Check("A rejected sleep attempt never consumes a turn", level.TurnNumber == turnBefore);
        Check("The rejection message matches the design doc", gameLoop.StatusMessages.Last() == "Your suffering prevents you from sleeping.");
    }

    private static void SleepIsRejectedOnARecurringDamageTile()
    {
        var player = NewEquipOfferTestPlayer("SleepRejectLavaTester", 957);
        player.Health.TakeDamage(10);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        level.Tiles[player.X, player.Y].FloorType = FloorType.Lava;

        bool started = gameLoop.HandleStartSleeping(level);

        Check("Sleep is rejected while standing on a recurring-damage tile", !started && !gameLoop.IsSleeping);
        Check("The rejection message matches the design doc", gameLoop.StatusMessages.Last() == "The burning ground makes sleep impossible.");
    }

    private static void SleepIsRejectedWhenAlreadyFullyRested()
    {
        var player = NewEquipOfferTestPlayer("SleepRejectFullTester", 958); // fresh Warrior: full HP, no mana pool at all
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        level.Tiles[player.X, player.Y].FloorType = FloorType.Normal; // see SleepIsRejectedWhileAHarmfulEffectIsActive's own note on why

        bool started = gameLoop.HandleStartSleeping(level);

        Check("Sleep is rejected when already fully rested", !started && !gameLoop.IsSleeping);
        Check("The rejection message matches the design doc", gameLoop.StatusMessages.Last() == "You are already fully rested.");
    }

    private static void StartingSleepAppliesFourTimesTheNormalRegenRate()
    {
        var player = NewEquipOfferTestPlayer("SleepRegenTester", 959);
        player.Health.TakeDamage(player.Health.Max - 1); // leave exactly 1 HP so sleep isn't instantly "fully rested"
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        level.Tiles[player.X, player.Y].FloorType = FloorType.Normal; // see SleepIsRejectedWhileAHarmfulEffectIsActive's own note on why

        gameLoop.HandleStartSleeping(level);
        Check("A valid sleep attempt begins sleeping", gameLoop.IsSleeping);

        double normalRate = ResourceRegenerationCalculator.CalculateHpRegenRate(player, inCombat: false);
        double sleepRate = ResourceRegenerationCalculator.CalculateHpRegenRate(player, inCombat: false, restMultiplier: RegenerationConfig.SleepRegenMultiplier);
        Check("The sleep regen rate is exactly 4x the normal out-of-combat rate",
            Math.Abs(sleepRate - (normalRate * RegenerationConfig.SleepRegenMultiplier)) < 0.0001);

        // Simulate the remaining ticks manually (the real scheduler loop isn't test-friendly --
        // see every other Handle*-style seam in this file) by healing to full directly.
        player.Health.Heal(player.Health.Max);
        gameLoop.CheckSleepInterrupts(level);
        Check("Reaching full HP (with no mana pool to wait on) ends sleep with the fully-rested message",
            !gameLoop.IsSleeping && gameLoop.StatusMessages.Last() == "You awaken fully rested, your health and strength restored.");
    }

    private static void TakingDamageWhileAsleepEndsSleepWithARecoveryMessage()
    {
        var player = NewEquipOfferTestPlayer("SleepDamageTester", 960);
        player.Health.TakeDamage(20);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        level.Tiles[player.X, player.Y].FloorType = FloorType.Normal; // see SleepIsRejectedWhileAHarmfulEffectIsActive's own note on why

        gameLoop.HandleStartSleeping(level);
        Check("Sleep begins successfully", gameLoop.IsSleeping);

        player.Health.TakeDamage(5); // simulate a monster's attack landing during an automatic sleep-turn
        gameLoop.CheckSleepInterrupts(level);

        Check("Taking damage while asleep ends sleep", !gameLoop.IsSleeping);
        Check("The trailing recovery sentence is appended -- no leading text is invented by this feature, since the normal combat message from whatever actually caused the damage is already in history",
            gameLoop.StatusMessages.Last() == "Your condition is scarcely improved.");
    }

    private static void AScheduledAmbushFiresAtTheRightTurnAndRegistersWithTheLevel()
    {
        var player = NewEquipOfferTestPlayer("SleepAmbushTester", 961);
        player.Health.TakeDamage(10);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        level.Tiles[player.X, player.Y].FloorType = FloorType.Normal; // see SleepIsRejectedWhileAHarmfulEffectIsActive's own note on why

        gameLoop.HandleStartSleeping(level);
        Check("Sleep begins successfully", gameLoop.IsSleeping);

        gameLoop.ForceSleepAmbushTurn(level.TurnNumber); // due immediately, regardless of what the real roll produced
        int actorsBefore = level.Actors.Count;

        gameLoop.CheckSleepInterrupts(level);

        Check("An ambush scheduled for the current turn fires and ends sleep", !gameLoop.IsSleeping);
        Check("At least one new monster was added to the level", level.Actors.Count > actorsBefore);
        Check("Every newly added actor is a real Monster", level.Actors.Skip(actorsBefore).All(a => a is Monster));
    }

    private static void HandleSleepInputKeyOpensHelpWithoutWaking()
    {
        var key = new ConsoleKeyInfo('h', ConsoleKey.H, false, false, false);
        Check("A key resolving to ShowHelp decides OpenHelp, never Wake",
            GameLoop.ResolveSleepInputOutcome(key) == GameLoop.SleepInputOutcome.OpenHelp);
    }

    private static void HandleSleepInputKeyIgnoresUnrecognizedKeys()
    {
        var key = new ConsoleKeyInfo('z', ConsoleKey.Z, false, false, false);
        Check("An unrecognized key decides StayAsleep",
            GameLoop.ResolveSleepInputOutcome(key) == GameLoop.SleepInputOutcome.StayAsleep);
    }

    private static void HandleSleepInputKeyWakesOnAnyOtherRecognizedCommand()
    {
        var moveKey = new ConsoleKeyInfo(' ', ConsoleKey.UpArrow, false, false, false);
        var waitKey = new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false);
        var escapeKey = new ConsoleKeyInfo((char)27, ConsoleKey.Escape, false, false, false);

        Check("A recognized movement key decides Wake", GameLoop.ResolveSleepInputOutcome(moveKey) == GameLoop.SleepInputOutcome.Wake);
        Check("Wait decides Wake too -- only ShowHelp and unrecognized keys don't", GameLoop.ResolveSleepInputOutcome(waitKey) == GameLoop.SleepInputOutcome.Wake);
        Check("Escape decides Wake, even though it has no PlayerCommand mapping at all", GameLoop.ResolveSleepInputOutcome(escapeKey) == GameLoop.SleepInputOutcome.Wake);
    }

    // --- Ability Proficiency System ------------------------------------------------------

    private static void ProficiencyRankThresholdBoundariesMatchSpec()
    {
        Check("0 proficiency is Novice", ProficiencyScaling.RankFor(0) == ProficiencyRank.Novice);
        Check("19 proficiency is still Novice", ProficiencyScaling.RankFor(19) == ProficiencyRank.Novice);
        Check("20 proficiency crosses into Practiced", ProficiencyScaling.RankFor(20) == ProficiencyRank.Practiced);
        Check("59 proficiency is still Practiced", ProficiencyScaling.RankFor(59) == ProficiencyRank.Practiced);
        Check("60 proficiency crosses into Proficient", ProficiencyScaling.RankFor(60) == ProficiencyRank.Proficient);
        Check("139 proficiency is still Proficient", ProficiencyScaling.RankFor(139) == ProficiencyRank.Proficient);
        Check("140 proficiency crosses into Expert", ProficiencyScaling.RankFor(140) == ProficiencyRank.Expert);
        Check("279 proficiency is still Expert", ProficiencyScaling.RankFor(279) == ProficiencyRank.Expert);
        Check("280 proficiency crosses into Master", ProficiencyScaling.RankFor(280) == ProficiencyRank.Master);
        Check("An arbitrarily large total stays Master, never a higher undefined rank", ProficiencyScaling.RankFor(1_000_000) == ProficiencyRank.Master);
    }

    private static void StandardMultiplierMatchesSpecAtAllFiveRanks()
    {
        Check("Standard multipliers run 90/95/100/110/120% across the five ranks -- Proficient's 100% matches today's unranked numbers exactly",
            Math.Abs(ProficiencyScaling.StandardMultiplier(ProficiencyRank.Novice) - 0.90) < 0.0001
            && Math.Abs(ProficiencyScaling.StandardMultiplier(ProficiencyRank.Practiced) - 0.95) < 0.0001
            && Math.Abs(ProficiencyScaling.StandardMultiplier(ProficiencyRank.Proficient) - 1.00) < 0.0001
            && Math.Abs(ProficiencyScaling.StandardMultiplier(ProficiencyRank.Expert) - 1.10) < 0.0001
            && Math.Abs(ProficiencyScaling.StandardMultiplier(ProficiencyRank.Master) - 1.20) < 0.0001);
    }

    private static void PercentagePointAdjustmentMatchesSpecAtAllFiveRanks()
    {
        Check("Accuracy/utility percentage-point adjustments run -10/-5/0/+5/+10 across the five ranks",
            Math.Abs(ProficiencyScaling.PercentagePointAdjustment(ProficiencyRank.Novice) - (-0.10)) < 0.0001
            && Math.Abs(ProficiencyScaling.PercentagePointAdjustment(ProficiencyRank.Practiced) - (-0.05)) < 0.0001
            && ProficiencyScaling.PercentagePointAdjustment(ProficiencyRank.Proficient) == 0.0
            && Math.Abs(ProficiencyScaling.PercentagePointAdjustment(ProficiencyRank.Expert) - 0.05) < 0.0001
            && Math.Abs(ProficiencyScaling.PercentagePointAdjustment(ProficiencyRank.Master) - 0.10) < 0.0001);
    }

    private static void SecondaryEffectChanceMatchesSpecAtAllFiveRanks()
    {
        Check("Secondary-effect application chance is 75% at Novice, 90% at Practiced, and caps at 100% for Proficient/Expert/Master",
            Math.Abs(ProficiencyScaling.SecondaryEffectChance(ProficiencyRank.Novice) - 0.75) < 0.0001
            && Math.Abs(ProficiencyScaling.SecondaryEffectChance(ProficiencyRank.Practiced) - 0.90) < 0.0001
            && ProficiencyScaling.SecondaryEffectChance(ProficiencyRank.Proficient) == 1.0
            && ProficiencyScaling.SecondaryEffectChance(ProficiencyRank.Expert) == 1.0
            && ProficiencyScaling.SecondaryEffectChance(ProficiencyRank.Master) == 1.0);
    }

    private static void D20AdjustmentMatchesSpecAtAllFiveRanks()
    {
        Check("d20-form utility adjustments run -2/-1/0/+1/+2 across the five ranks",
            ProficiencyScaling.D20Adjustment(ProficiencyRank.Novice) == -2
            && ProficiencyScaling.D20Adjustment(ProficiencyRank.Practiced) == -1
            && ProficiencyScaling.D20Adjustment(ProficiencyRank.Proficient) == 0
            && ProficiencyScaling.D20Adjustment(ProficiencyRank.Expert) == 1
            && ProficiencyScaling.D20Adjustment(ProficiencyRank.Master) == 2);
    }

    private static void BespokeProficiencyTablesMatchSpec()
    {
        Check("Pick Lock's break chance runs 45/42/40/30/20% across the five ranks -- Proficient's 40% matches the old fixed value",
            Math.Abs(ProficiencyScaling.PickLockBreakChance(ProficiencyRank.Novice) - 0.45) < 0.0001
            && Math.Abs(ProficiencyScaling.PickLockBreakChance(ProficiencyRank.Practiced) - 0.42) < 0.0001
            && Math.Abs(ProficiencyScaling.PickLockBreakChance(ProficiencyRank.Proficient) - 0.40) < 0.0001
            && Math.Abs(ProficiencyScaling.PickLockBreakChance(ProficiencyRank.Expert) - 0.30) < 0.0001
            && Math.Abs(ProficiencyScaling.PickLockBreakChance(ProficiencyRank.Master) - 0.20) < 0.0001);

        Check("Sneak's detection radius runs 5/5/4/3/2 across the five ranks -- Proficient's 4 matches the old fixed BaseRadius/2",
            ProficiencyScaling.SneakDetectionRadius(ProficiencyRank.Novice) == 5
            && ProficiencyScaling.SneakDetectionRadius(ProficiencyRank.Practiced) == 5
            && ProficiencyScaling.SneakDetectionRadius(ProficiencyRank.Proficient) == 4
            && ProficiencyScaling.SneakDetectionRadius(ProficiencyRank.Expert) == 3
            && ProficiencyScaling.SneakDetectionRadius(ProficiencyRank.Master) == 2);

        Check("Pick Pocket's bonus chance runs 20/22/25/30/35% across the five ranks -- Proficient's 25% matches the old fixed value",
            Math.Abs(ProficiencyScaling.PickPocketBonusChance(ProficiencyRank.Novice) - 0.20) < 0.0001
            && Math.Abs(ProficiencyScaling.PickPocketBonusChance(ProficiencyRank.Practiced) - 0.22) < 0.0001
            && Math.Abs(ProficiencyScaling.PickPocketBonusChance(ProficiencyRank.Proficient) - 0.25) < 0.0001
            && Math.Abs(ProficiencyScaling.PickPocketBonusChance(ProficiencyRank.Expert) - 0.30) < 0.0001
            && Math.Abs(ProficiencyScaling.PickPocketBonusChance(ProficiencyRank.Master) - 0.35) < 0.0001);

        var novicePoison = ProficiencyScaling.PoisonWeaponProfile(ProficiencyRank.Novice);
        var proficientPoison = ProficiencyScaling.PoisonWeaponProfile(ProficiencyRank.Proficient);
        var masterPoison = ProficiencyScaling.PoisonWeaponProfile(ProficiencyRank.Master);
        Check("Poison Weapon's profile matches the old fixed values at Proficient (10-turn window, 30% chance, 4-turn duration, 2 tick damage)",
            proficientPoison is { Window: 10, Chance: 0.30, Duration: 4, TickDamage: 2 });
        Check("Poison Weapon's profile improves window and proc chance from Novice up through Master",
            novicePoison.Window < masterPoison.Window && novicePoison.Chance < masterPoison.Chance);

        Check("Execution Call's threshold multiplier runs 9/9.5/10/11/12 across the five ranks -- Proficient's 10 matches the old fixed value",
            Math.Abs(ProficiencyScaling.ExecutionCallThresholdMultiplier(ProficiencyRank.Novice) - 9.0) < 0.0001
            && Math.Abs(ProficiencyScaling.ExecutionCallThresholdMultiplier(ProficiencyRank.Practiced) - 9.5) < 0.0001
            && Math.Abs(ProficiencyScaling.ExecutionCallThresholdMultiplier(ProficiencyRank.Proficient) - 10.0) < 0.0001
            && Math.Abs(ProficiencyScaling.ExecutionCallThresholdMultiplier(ProficiencyRank.Expert) - 11.0) < 0.0001
            && Math.Abs(ProficiencyScaling.ExecutionCallThresholdMultiplier(ProficiencyRank.Master) - 12.0) < 0.0001);
    }

    /// <summary>Cross-checks against the spec's own two worked examples so a silent formula regression can't hide behind individually-correct-looking table values.</summary>
    private static void AptitudeAndCatalogLevelLearningRatesMatchSpecWorkedExamples()
    {
        Check("Aptitude learning rate at Strength 15 is 1.20 (the 12-17 band)", Math.Abs(ProficiencyScaling.AptitudeLearningRate(15) - 1.20) < 0.0001);
        Check("Catalog-level learning modifier is 1.00 for a level-1 ability (Bash)", Math.Abs(ProficiencyScaling.CatalogLevelLearningModifier(1) - 1.00) < 0.0001);
        Check("Catalog-level learning modifier is 1.50 for a level-25 ability (Executioner)", Math.Abs(ProficiencyScaling.CatalogLevelLearningModifier(25) - 1.50) < 0.0001);

        double bashPoints = ProficiencyConfig.SuccessPoints * ProficiencyScaling.AptitudeLearningRate(15) * ProficiencyScaling.CatalogLevelLearningModifier(1);
        double executionerPoints = ProficiencyConfig.SuccessPoints * ProficiencyScaling.AptitudeLearningRate(15) * ProficiencyScaling.CatalogLevelLearningModifier(25);
        Check("Spec worked example: a Strength-15 Bash success awards exactly 2.4 points", Math.Abs(bashPoints - 2.4) < 0.0001);
        Check("Spec worked example: a Strength-15, level-25 Executioner success awards exactly 3.6 points", Math.Abs(executionerPoints - 3.6) < 0.0001);
    }

    private static void LuckyInsightChanceFormulaClampsAndScalesWithLuck()
    {
        Check("Lucky insight chance at the average-roll baseline (Luck 10) is exactly 2%",
            Math.Abs(ProficiencyScaling.LuckyInsightChance(10) - 0.02) < 0.0001);
        Check("Higher Luck raises the chance, lower Luck lowers it",
            ProficiencyScaling.LuckyInsightChance(18) > ProficiencyScaling.LuckyInsightChance(10)
            && ProficiencyScaling.LuckyInsightChance(1) < ProficiencyScaling.LuckyInsightChance(10));
        Check("The chance never exceeds 8% or drops below 1%, however extreme the adjusted Luck value",
            ProficiencyScaling.LuckyInsightChance(1000) <= 0.08 && ProficiencyScaling.LuckyInsightChance(-1000) >= 0.01);
    }

    private static void ChallengeMultiplierMatchesSpecGapBuckets()
    {
        Check("A target at or above the player's own level earns full credit", ProficiencyScaling.ChallengeMultiplier(targetLevel: 10, playerLevel: 5) == 1.0);
        Check("A target up to 5 levels below the player still earns full credit", ProficiencyScaling.ChallengeMultiplier(targetLevel: 5, playerLevel: 10) == 1.0);
        Check("A target 6-10 levels below earns half credit", ProficiencyScaling.ChallengeMultiplier(targetLevel: 2, playerLevel: 10) == 0.5);
        Check("A target more than 10 levels below earns no credit at all", ProficiencyScaling.ChallengeMultiplier(targetLevel: 1, playerLevel: 20) == 0.0);
    }

    private static void RankScalingNeutralReproducesTodaysUnscaledNumbers()
    {
        var neutral = RankScaling.Neutral;
        Check("RankScaling.Neutral is Proficient rank", neutral.Rank == ProficiencyRank.Proficient);
        Check("Neutral's standard multiplier, percentage-point adjustment, secondary-effect chance, and d20 adjustment are all no-ops",
            Math.Abs(neutral.StandardMultiplier - 1.0) < 0.0001 && neutral.PercentagePointAdjustment == 0.0
            && Math.Abs(neutral.SecondaryEffectChance - 1.0) < 0.0001 && neutral.D20Adjustment == 0);
        Check("Neutral never changes a duration or magnitude value -- any caster without a tracked rank (every monster) sees exactly today's numbers",
            neutral.ScaleDuration(10) == 10 && neutral.ScaleMagnitude(10) == 10 && neutral.ScaleMagnitude(-10) == -10);
    }

    private static void RankScalingScaleDurationRoundsAndNeverGoesBelowOne()
    {
        var novice = new RankScaling(ProficiencyRank.Novice);
        var master = new RankScaling(ProficiencyRank.Master);
        Check("A Novice-rank 1-turn duration never rounds down to 0 -- floored at 1", novice.ScaleDuration(1) == 1);
        Check("A Novice-rank 10-turn duration scales down to 9 (90%)", novice.ScaleDuration(10) == 9);
        Check("A Master-rank 10-turn duration scales up to 12 (120%)", master.ScaleDuration(10) == 12);
    }

    private static void RankScalingScaleMagnitudeHandlesNegativeValuesCorrectly()
    {
        var novice = new RankScaling(ProficiencyRank.Novice);
        var master = new RankScaling(ProficiencyRank.Master);
        Check("A Novice-rank -10 debuff magnitude scales to -9, not flipping sign or flooring at 0", novice.ScaleMagnitude(-10) == -9);
        Check("A Master-rank -10 debuff magnitude scales to -12", master.ScaleMagnitude(-10) == -12);
        Check("A Novice-rank +10 magnitude scales down to +9", novice.ScaleMagnitude(10) == 9);
    }

    private static void EveryRankedSkillAndSpellHasAGoverningAttributeAndEveryUnrankedOneDoesNot()
    {
        Check("Every ranked skill declares a governing attribute",
            SkillCatalog.All.Where(s => !string.IsNullOrEmpty(s.ProficiencyId)).All(s => s.GoverningAttribute != null));
        Check("Every unranked skill (the one-shot permanent passives, plus Dual Wield/Riposte) has no governing attribute",
            SkillCatalog.All.Where(s => string.IsNullOrEmpty(s.ProficiencyId)).All(s => s.GoverningAttribute == null));
        Check("Exactly 28 of the 34 skills are ranked -- Hardy Constitution/Stalwart Defender/Fleet Footed/Dual Wield/Riposte/Steadfast stay unranked (Trip and 5 of the 6 new Priest skills, added by the Prone/Knockdown System and New Priest Skill Progression respectively, are ranked)",
            SkillCatalog.All.Count == 34 && SkillCatalog.All.Count(s => !string.IsNullOrEmpty(s.ProficiencyId)) == 28);

        Check("Every ranked class-exclusive spell (Mage-only/Priest-only) declares a governing attribute",
            SpellCatalog.All.Where(s => !string.IsNullOrEmpty(s.ProficiencyId) && !s.AllSpellcasters).All(s => s.GoverningAttribute != null));
        Check("Exactly 43 of the 44 spells are ranked -- only the shared, always-succeeding Identify spell stays unranked (Tremor, added by the Prone/Knockdown System, is ranked)",
            SpellCatalog.All.Count == 44 && SpellCatalog.All.Count(s => !string.IsNullOrEmpty(s.ProficiencyId)) == 43);
        Check("The shared Identify spell is the one unranked spell, per spec section 21",
            string.IsNullOrEmpty(SpellCatalog.Identify.ProficiencyId) && SpellCatalog.Identify.GoverningAttribute == null);
    }

    private static void SharedSpellsLeaveGoverningAttributeNullForDynamicResolution()
    {
        var rankedSharedSpells = SpellCatalog.All.Where(s => s.AllSpellcasters && !string.IsNullOrEmpty(s.ProficiencyId)).ToList();
        Check("At least one ranked shared spell exists to exercise this rule (Minor Ward, Sap Strength, ...)", rankedSharedSpells.Count > 0);
        Check("Every ranked shared spell leaves GoverningAttribute null -- resolved dynamically as the caster's own CharacterClass.ManaStat at cast time instead",
            rankedSharedSpells.All(s => s.GoverningAttribute == null));
    }

    /// <summary>Never rolls a lucky insight, so every point delta is exactly the deterministic base/aptitude/level formula -- see NeverLuckyRandom.</summary>
    private class NeverLuckyRandom : Random
    {
        public override double NextDouble() => 0.999;
    }

    private static Player BuildProficiencyTestPlayer(int strength = 10, int luck = 10)
    {
        var player = new Player("ProficiencyTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(1)));
        var strengthBlock = player.Stats.Get(PrimaryAttribute.Strength);
        strengthBlock.BaseValue = strength;
        strengthBlock.ClassModifier = 0;
        strengthBlock.EquipmentModifier = 0;
        var luckBlock = player.Stats.Get(PrimaryAttribute.Luck);
        luckBlock.BaseValue = luck;
        luckBlock.ClassModifier = 0;
        luckBlock.EquipmentModifier = 0;
        return player;
    }

    private static void ProficiencyTrackerAwardsPointsAndRanksUpExactlyAtThreshold()
    {
        var player = BuildProficiencyTestPlayer();
        var rng = new NeverLuckyRandom();
        const string id = "test.proficiency.tracker.rankup";
        var messages = new List<(string Text, ConsoleColor Color)>();
        void AddMessage(string text, ConsoleColor color) => messages.Add((text, color));

        for (int i = 0; i < 9; i++)
        {
            var result = new SpellCastResult { Success = true };
            ProficiencyTracker.Process(player, id, "Test Ability", PrimaryAttribute.Strength, abilityLevel: 1, result, targetMonsterLevel: null, rng, AddMessage);
        }
        Check("After 9 successful uses at a 1.0 aptitude/level rate (2.0 points each = 18.0), the ability is still Novice",
            player.RankOf(id) == ProficiencyRank.Novice);
        Check("No rank-up message fired yet", messages.Count == 0);

        var tenthResult = new SpellCastResult { Success = true };
        ProficiencyTracker.Process(player, id, "Test Ability", PrimaryAttribute.Strength, abilityLevel: 1, tenthResult, targetMonsterLevel: null, rng, AddMessage);

        Check("The 10th successful use crosses exactly into Practiced (18.0 + 2.0 = 20.0)", player.RankOf(id) == ProficiencyRank.Practiced);
        Check("Crossing a threshold produces exactly one yellow rank-up message naming the new rank",
            messages.Count == 1 && messages[0].Color == ConsoleColor.Yellow && messages[0].Text.Contains("Practiced"));
    }

    private static void ProficiencyTrackerNeverAwardsPointsOnAFailedCast()
    {
        var player = BuildProficiencyTestPlayer();
        const string id = "test.proficiency.tracker.failedcast";
        var result = new SpellCastResult { Success = false };
        ProficiencyTracker.Process(player, id, "Test Ability", PrimaryAttribute.Strength, abilityLevel: 1, result, targetMonsterLevel: null, new NeverLuckyRandom(), (_, _) => { });

        Check("A cast that never resolved (out of mana, invalid target, on cooldown, ...) awards no proficiency at all",
            !player.Proficiencies.ContainsKey(id));
    }

    private static void ProficiencyTrackerStopsAwardingOnceMaster()
    {
        var player = BuildProficiencyTestPlayer();
        const string id = "test.proficiency.tracker.master";
        player.Proficiencies[id] = new AbilityProficiency { Proficiency = ProficiencyConfig.MasterThreshold };
        Check("Setup: the ability starts already at Master", player.RankOf(id) == ProficiencyRank.Master);

        var messages = new List<string>();
        var result = new SpellCastResult { Success = true };
        ProficiencyTracker.Process(player, id, "Test Ability", PrimaryAttribute.Strength, abilityLevel: 1, result, targetMonsterLevel: null, new NeverLuckyRandom(), (text, _) => messages.Add(text));

        Check("An already-Master ability's proficiency total never increases further", player.Proficiencies[id].Proficiency == ProficiencyConfig.MasterThreshold);
        Check("No further messages are produced once Master", messages.Count == 0);
    }

    private static void ProficiencyTrackerLuckyInsightAddsAFlatBonusAndNeverSkipsARank()
    {
        var player = BuildProficiencyTestPlayer();
        var rng = new AlwaysSucceedRandom(); // NextDouble() == 0.0 always -- guarantees every lucky-insight roll succeeds
        const string id = "test.proficiency.tracker.lucky";
        var messages = new List<(string Text, ConsoleColor Color)>();

        for (int i = 0; i < 4; i++)
        {
            var result = new SpellCastResult { Success = true };
            ProficiencyTracker.Process(player, id, "Test Ability", PrimaryAttribute.Strength, abilityLevel: 1, result, targetMonsterLevel: null, rng,
                (text, color) => messages.Add((text, color)));
        }

        Check("4 lucky successes (2.0 base + 3.0 flat lucky bonus = 5.0 each) total exactly 20.0, crossing into Practiced -- never skipping straight to a higher rank",
            player.RankOf(id) == ProficiencyRank.Practiced);
        Check("Every lucky-insight use produces exactly one yellow message (4 calls -> 4 yellow lines)",
            messages.Count == 4 && messages.All(m => m.Color == ConsoleColor.Yellow));
        Check("The final message announces the rank-up combined with the lucky insight, not two separate lines",
            messages[^1].Text.Contains("Practiced"));
    }

    private static void ProficiencyTrackerBailsOnAFailedStatBasedRollWithoutAwardingPoints()
    {
        var player = BuildProficiencyTestPlayer();
        const string id = "test.proficiency.tracker.failedroll";
        var result = new SpellCastResult { Success = true, FailedIdentifyItemName = "a mystery item" };
        ProficiencyTracker.Process(player, id, "Test Ability", PrimaryAttribute.Strength, abilityLevel: 1, result, targetMonsterLevel: null, new NeverLuckyRandom(), (_, _) => { });

        Check("A failed stat-based roll (Thief Identify) is a non-legitimate use -- no points awarded at all, per spec section 16",
            !player.Proficiencies.ContainsKey(id));
    }

    private static void BashStunBecomesProbabilisticAtNoviceRankButAlwaysAppliesAtMaster()
    {
        var level = BuildOpenLevel(5, 5);
        var rng = new Random(7);
        var caster = Monster.CreateRandom(0, 0, 1, rng);

        int noviceStunCount = 0;
        const int samples = 300;
        for (int i = 0; i < samples; i++)
        {
            var target = Monster.CreateRandom(1, 0, 1, rng);
            var context = new SpellCastingContext(caster, level, level.TurnNumber, rng)
            {
                AffectedActors = new List<Actor> { target },
                RankScaling = new RankScaling(ProficiencyRank.Novice)
            };
            new StunEffect(duration: 1).Apply(context, new SpellCastResult());
            if (target.StunnedUntilTurn > level.TurnNumber)
            {
                noviceStunCount++;
            }
        }
        Check("At Novice rank (75% secondary-effect chance), the stun sometimes fails to apply even though it always would have pre-feature",
            noviceStunCount > 0 && noviceStunCount < samples);

        bool everyMasterStunApplied = true;
        for (int i = 0; i < 50; i++)
        {
            var target = Monster.CreateRandom(2, 0, 1, rng);
            var context = new SpellCastingContext(caster, level, level.TurnNumber, rng)
            {
                AffectedActors = new List<Actor> { target },
                RankScaling = new RankScaling(ProficiencyRank.Master)
            };
            new StunEffect(duration: 1).Apply(context, new SpellCastResult());
            if (target.StunnedUntilTurn <= level.TurnNumber)
            {
                everyMasterStunApplied = false;
                break;
            }
        }
        Check("At Master rank (100% secondary-effect chance), the stun always applies", everyMasterStunApplied);
    }

    private static void MessageLogAddDefaultsToWhiteAndAcceptsAnExplicitColor()
    {
        var log = new MessageLog();
        log.Add("A plain status message.");
        log.Add("A lucky-insight message.", ConsoleColor.Yellow);

        Check("A message added with no explicit color defaults to White", log.History[0].Color == ConsoleColor.White);
        Check("A message added with an explicit color keeps it", log.History[1].Color == ConsoleColor.Yellow);
        Check("GetRecent preserves color alongside text",
            log.GetRecent(2)[1].Color == ConsoleColor.Yellow && log.GetRecent(2)[1].Text == "A lucky-insight message.");
    }

    private static void RenderStatusBarPreservesPerMessageColorAcrossWrappedLines()
    {
        var shortMessages = new List<MessageEntry>
        {
            new("A short white message.", ConsoleColor.White),
            new("A short yellow message.", ConsoleColor.Yellow)
        };
        var wrapped = Renderer.BuildWrappedColoredLines(shortMessages, width: 80, maxLines: 6);
        Check("Each short (unwrapped) message produces exactly one line, tagged with its own color",
            wrapped.Count == 2 && wrapped[0].Color == ConsoleColor.White && wrapped[1].Color == ConsoleColor.Yellow);

        var longMessage = new List<MessageEntry> { new(new string('x', 200), ConsoleColor.Yellow) };
        var wrappedLong = Renderer.BuildWrappedColoredLines(longMessage, width: 40, maxLines: 6);
        Check("A single long message that wraps into multiple lines keeps every resulting line tagged with the same color",
            wrappedLong.Count > 1 && wrappedLong.All(l => l.Color == ConsoleColor.Yellow));

        var manyMessages = Enumerable.Range(0, 10).Select(i => new MessageEntry($"Message {i}", ConsoleColor.White)).ToList();
        var wrappedCapped = Renderer.BuildWrappedColoredLines(manyMessages, width: 80, maxLines: 3);
        Check("Overflowing the max line count keeps only the most recent lines, oldest dropped first",
            wrappedCapped.Count == 3 && wrappedCapped[0].Text == "Message 7" && wrappedCapped[^1].Text == "Message 9");
    }

    private static Dictionary<PrimaryAttribute, StatBlockData> BuildFullStatsData() => new()
    {
        [PrimaryAttribute.Strength] = new StatBlockData { BaseValue = 10 },
        [PrimaryAttribute.Constitution] = new StatBlockData { BaseValue = 10 },
        [PrimaryAttribute.Agility] = new StatBlockData { BaseValue = 10 },
        [PrimaryAttribute.Wisdom] = new StatBlockData { BaseValue = 10 },
        [PrimaryAttribute.Knowledge] = new StatBlockData { BaseValue = 10 },
        [PrimaryAttribute.Charisma] = new StatBlockData { BaseValue = 10 },
        [PrimaryAttribute.Luck] = new StatBlockData { BaseValue = 10 }
    };

    private static void PlayerAbilityStateSurvivesASaveLoadRoundTrip()
    {
        var data = new SaveData
        {
            ClassName = CharacterClass.Thief.Name,
            RaceName = Race.Human.Name,
            Level = 5,
            Stats = BuildFullStatsData(),
            KnownSkillNames = new List<string> { SkillCatalog.Sneak.Name, SkillCatalog.Backstab.Name },
            KnownSpellNames = new List<string> { SpellCatalog.MagicMissile.Name },
            SkillCooldownsByName = new Dictionary<string, int> { [SkillCatalog.Backstab.Name] = 42 },
            SpellCooldownsByName = new Dictionary<string, int> { [SpellCatalog.MagicMissile.Name] = 99 },
            PlayerActiveEffects = new List<ActiveEffectData>
            {
                new() { SourceSpellName = "Test Buff", ExpiresOnTurn = 100, ModifiedStat = Stat.PhysicalAttack, StatAmount = 5 }
            },
            Proficiencies = new Dictionary<string, AbilityProficiencyData>
            {
                [SkillCatalog.Backstab.ProficiencyId] = new AbilityProficiencyData { Proficiency = 75, ValidUses = 10, SuccessfulUses = 8, FailedUses = 2, LuckyInsights = 1 }
            }
        };

        var restored = SaveManager.ToPlayer(data);

        Check("KnownSkillNames restores exactly the persisted skill set -- no more, no less",
            restored.KnownSkills.Count == 2 && restored.KnownSkills.Contains(SkillCatalog.Sneak) && restored.KnownSkills.Contains(SkillCatalog.Backstab));
        Check("KnownSpellNames restores exactly the persisted spell set",
            restored.KnownSpells.Count == 1 && restored.KnownSpells.Contains(SpellCatalog.MagicMissile));
        Check("Skill cooldowns restore by name resolution",
            restored.SkillCooldowns.TryGetValue(SkillCatalog.Backstab, out int backstabCooldown) && backstabCooldown == 42);
        Check("Spell cooldowns restore by name resolution",
            restored.SpellCooldowns.TryGetValue(SpellCatalog.MagicMissile, out int magicMissileCooldown) && magicMissileCooldown == 99);
        Check("An active buff/DoT survives the round trip",
            restored.ActiveEffects.Count == 1 && restored.ActiveEffects[0].SourceSpellName == "Test Buff" && restored.ActiveEffects[0].StatAmount == 5);
        Check("Ability Proficiency System rank data survives the round trip",
            restored.RankOf(SkillCatalog.Backstab.ProficiencyId) == ProficiencyRank.Proficient
            && restored.Proficiencies[SkillCatalog.Backstab.ProficiencyId].ValidUses == 10);
    }

    private static void OldSaveDataWithoutAbilityStateFallsBackToLevelBasedSkillRederivation()
    {
        var data = new SaveData
        {
            ClassName = CharacterClass.Warrior.Name,
            RaceName = Race.Human.Name,
            Level = 16, // crosses several deterministic Warrior skill-grant levels (1, 2, 3, 6, 13, 15, 16)
            Stats = BuildFullStatsData()
            // KnownSkillNames/KnownSpellNames/Proficiencies deliberately left null -- a pre-feature save.
        };

        var restored = SaveManager.ToPlayer(data);

        var expectedSkills = SkillCatalog.All.Where(s => s.Level <= 16 && s.AllowedClasses.Contains(CharacterClass.Warrior)).ToList();
        Check("An old save with no persisted skill list falls back to re-deriving every deterministic level-based skill grant up to the restored level",
            expectedSkills.Count == restored.KnownSkills.Count && expectedSkills.All(s => restored.KnownSkills.Contains(s)));
        Check("A never-yet-used ranked ability restored this way still reports Novice, exactly like a fresh character",
            restored.RankOf(SkillCatalog.Bash.ProficiencyId) == ProficiencyRank.Novice);
    }

    // --- Shield-Based Bash Skill ----------------------------------------------------------

    /// <summary>Guarantees any hit-chance/application-chance roll succeeds (NextDouble always 0.0, below any real chance) and zeroes out PhysicalAttack's -1/0/+1 damage variance (Next(int,int) always 0, which is in range for that call's [-1, 2) bounds) -- lets these tests assert an exact expected damage number instead of a +-1 range.</summary>
    private class DeterministicCombatRandom : Random
    {
        public override double NextDouble() => 0.0;
        public override int Next(int minValue, int maxValue) => 0;
    }

    /// <summary>Fresh, independent Warrior + 0-defense/1000-HP target/level for each Bash test -- no shared mutable state (cooldowns, equipment) leaks between tests.</summary>
    private static (Player Player, Monster Target, Level Level) BuildBashTestScenario()
    {
        var level = BuildOpenLevel(3, 3);
        var player = new Player("BashTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(1)))
        {
            X = 1,
            Y = 1
        };
        level.Actors.Add(player);

        var target = Monster.Restore(BaseRestoreData("bash-target", attack: 0, defense: 0));
        target.X = 2;
        target.Y = 1;
        level.Actors.Add(target);

        return (player, target, level);
    }

    private static SpellCastResult CastBash(Player player, Monster target, Level level, Random rng) =>
        SkillCaster.Cast(SkillCatalog.Bash, new SpellCastingContext(player, level, level.TurnNumber, rng) { TargetActor = target });

    private static void BashRequiresAnEquippedShieldInEitherHand()
    {
        var (player, target, level) = BuildBashTestScenario();
        var result = CastBash(player, target, level, new DeterministicCombatRandom());

        Check("Bash fails outright with no shield equipped in either hand", !result.Success && result.FailureReason == "Bash requires a shield.");
        Check("A failed Bash attempt never starts its cooldown", !player.SkillCooldowns.ContainsKey(SkillCatalog.Bash));
    }

    private static void BashWorksWithAShieldInThePrimaryHand()
    {
        var (player, target, level) = BuildBashTestScenario();
        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.Shield);

        var result = CastBash(player, target, level, new DeterministicCombatRandom());

        Check("Bash succeeds with a shield equipped in the Primary Hand", result.Success);
    }

    private static void BashWorksWithAShieldInTheOffHand()
    {
        var (player, target, level) = BuildBashTestScenario();
        player.EquipInSlot(EquipmentSlot.OffHand, Items.Shield);

        var result = CastBash(player, target, level, new DeterministicCombatRandom());

        Check("Bash succeeds with a shield equipped in the Off-hand", result.Success);
    }

    private static void ShieldDefenseBonusIncreasesBashDamageByExactlyThatAmount()
    {
        var (player, target, level) = BuildBashTestScenario();
        int basePower = player.BasePhysicalAttackPower;
        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.Shield); // Wooden Shield, DefenseBonus +2

        var result = CastBash(player, target, level, new DeterministicCombatRandom());

        Check("Bash's damage equals the caster's normal attack power plus exactly the equipped shield's DefenseBonus (+2), against a 0-defense target with variance pinned to 0",
            result.Success && result.DamageDealt == basePower + Items.Shield.DefenseBonus);
    }

    private static void AStrongerShieldProducesMoreBashDamageThanAWeakerShield()
    {
        var (woodenPlayer, woodenTarget, woodenLevel) = BuildBashTestScenario();
        woodenPlayer.EquipInSlot(EquipmentSlot.PrimaryHand, Items.Shield); // +2
        var woodenResult = CastBash(woodenPlayer, woodenTarget, woodenLevel, new DeterministicCombatRandom());

        var (towerPlayer, towerTarget, towerLevel) = BuildBashTestScenario();
        towerPlayer.EquipInSlot(EquipmentSlot.PrimaryHand, Items.TowerShield); // +6
        var towerResult = CastBash(towerPlayer, towerTarget, towerLevel, new DeterministicCombatRandom());

        Check("A Tower Shield (+6) produces more Bash damage than a Wooden Shield (+2)",
            woodenResult.Success && towerResult.Success && towerResult.DamageDealt > woodenResult.DamageDealt);
        Check("The exact difference matches the two shields' DefenseBonus gap (+4)",
            towerResult.DamageDealt - woodenResult.DamageDealt == Items.TowerShield.DefenseBonus - Items.Shield.DefenseBonus);
    }

    private static void NonShieldArmorDoesNotContributeTheBashBonus()
    {
        var (player, target, level) = BuildBashTestScenario();
        int basePower = player.BasePhysicalAttackPower;
        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.Shield); // +2 -- satisfies RequiresShield
        player.EquipInSlot(EquipmentSlot.Body, Items.LeatherArmor); // +1 DefenseBonus, but NOT a shield

        var result = CastBash(player, target, level, new DeterministicCombatRandom());

        Check("Body armor's own DefenseBonus never stacks onto Bash's shield-only bonus -- only the equipped shield's +2 applies, not +2 and +1 combined",
            result.Success && result.DamageDealt == basePower + Items.Shield.DefenseBonus);
    }

    private static void OrdinaryAttacksAndOtherWeaponDamageEffectSkillsIgnoreTheShieldBonus()
    {
        var (player, target, level) = BuildBashTestScenario();
        int basePower = player.BasePhysicalAttackPower;
        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.Shield); // +2

        var rng = new DeterministicCombatRandom();
        var ordinaryAttack = player.PhysicalAttack(target, rng);
        Check("An ordinary (non-skill) attack is completely unaffected by a shield's DefenseBonus even while one is equipped",
            ordinaryAttack.Hit && ordinaryAttack.Damage == basePower);

        // Kick: another Warrior WeaponDamageEffect skill, but without addShieldDefenseBonus.
        var kickContext = new SpellCastingContext(player, level, level.TurnNumber, rng) { TargetActor = target };
        var kickResult = SkillCaster.Cast(SkillCatalog.Kick, kickContext);
        Check("A different WeaponDamageEffect skill that doesn't opt into addShieldDefenseBonus (Kick) is also unaffected by the equipped shield",
            kickResult.Success && kickResult.DamageDealt == basePower);
    }

    private static void CastersPhysicalAttackPowerIsRestoredAfterBashResolves()
    {
        var (player, target, level) = BuildBashTestScenario();
        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.TowerShield); // +6 -- the largest bonus, so any leak would be obvious
        int powerBeforeCast = player.BasePhysicalAttackPower;

        CastBash(player, target, level, new DeterministicCombatRandom());

        Check("BasePhysicalAttackPower is restored to its exact pre-cast value once Bash resolves, not left permanently boosted",
            player.BasePhysicalAttackPower == powerBeforeCast);
    }

    private static void BashUnlockLevelDoublesForPriestFlooredAtFive()
    {
        Check("Bash's base (Warrior) unlock level is unchanged at 1",
            SkillCatalog.Bash.LevelFor(CharacterClass.Warrior) == 1);
        Check("Bash unlocks for Priest at double the Warrior level, floored at 5 (double of 1 is 2, floored up to 5)",
            SkillCatalog.Bash.LevelFor(CharacterClass.Priest) == 5);
    }

    private static void PriestAndWarriorEachGainBashAtTheirOwnUnlockLevelNotTheOthers()
    {
        var warrior = new Player("BashWarrior", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(1)));
        Check("A freshly created level-1 Warrior already knows Bash (granted by the Player constructor itself)",
            warrior.KnownSkills.Contains(SkillCatalog.Bash));

        var priest = new Player("BashPriest", CharacterClass.Priest, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Priest, new Random(2)));
        Check("A freshly created level-1 Priest does not know Bash yet -- their own unlock level is 5",
            !priest.KnownSkills.Contains(SkillCatalog.Bash));

        for (int level = 2; level <= 4; level++)
        {
            priest.GrantSkillsForLevel(level);
        }
        Check("A Priest still doesn't know Bash at level 4, one short of their own unlock level",
            !priest.KnownSkills.Contains(SkillCatalog.Bash));

        priest.GrantSkillsForLevel(5);
        Check("A Priest gains Bash exactly at level 5, their own doubled-and-floored unlock level",
            priest.KnownSkills.Contains(SkillCatalog.Bash));
    }

    private static void ThiefAndMageNeverGetBash()
    {
        Check("Bash's allowed classes are exactly Warrior and Priest -- Thief and Mage are excluded",
            SkillCatalog.Bash.AllowedClasses.Contains(CharacterClass.Warrior)
            && SkillCatalog.Bash.AllowedClasses.Contains(CharacterClass.Priest)
            && !SkillCatalog.Bash.AllowedClasses.Contains(CharacterClass.Thief)
            && !SkillCatalog.Bash.AllowedClasses.Contains(CharacterClass.Mage));
    }

    // --- Prone and Knockdown System --------------------------------------------------------

    /// <summary>Guarantees a miss (NextDouble always just above the 0.99 hit-chance ceiling), unlike DeterministicCombatRandom's guaranteed hit -- lets a test force Bash's attack roll to fail.</summary>
    private class ForcedMissRandom : Random
    {
        public override double NextDouble() => 0.999;
        public override int Next(int minValue, int maxValue) => 0;
    }

    private static void KnockdownResolverAppliesSetsProneAndConsumesABankedReadyAction()
    {
        var scheduler = new TurnScheduler();
        var target = Monster.Restore(BaseRestoreData("knockdown-target", attack: 0, defense: 0));
        target.Energy = 150; // a ready action banked, plus some overshoot

        var outcome = KnockdownResolver.TryApply(target, turnNumber: 10, scheduler, new Random(1));

        Check("TryApply reports Applied for a valid, non-immune, non-prone target", outcome == KnockdownOutcome.Applied);
        Check("The target is now prone", target.IsProne);
        Check("Exactly one ready action (100 energy) is removed, leaving the overshoot intact", target.Energy == 50);
    }

    private static void KnockdownResolverDoesNotPenalizeAnAlreadyProneTargetTwice()
    {
        var scheduler = new TurnScheduler();
        var target = Monster.Restore(BaseRestoreData("already-prone-target", attack: 0, defense: 0));
        target.IsProne = true;
        target.Energy = 150;

        var outcome = KnockdownResolver.TryApply(target, turnNumber: 10, scheduler, new Random(1));

        Check("TryApply reports AlreadyProne for a target already knocked down", outcome == KnockdownOutcome.AlreadyProne);
        Check("No additional ready-action penalty is imposed on a repeat knockdown", target.Energy == 150);
    }

    private static void KnockdownResolverIsResistedByCrowdControlImmunity()
    {
        var scheduler = new TurnScheduler();
        var target = Monster.Restore(BaseRestoreData("cc-immune-target", attack: 0, defense: 0));
        target.CcImmuneUntilTurn = 20;

        var outcome = KnockdownResolver.TryApply(target, turnNumber: 10, scheduler, new Random(1));

        Check("TryApply reports Immune while the target's Berserker-Rage-style CC immunity is active", outcome == KnockdownOutcome.Immune);
        Check("An immune target is never actually set prone", !target.IsProne);
    }

    private static void EnvironmentalFallBypassesCrowdControlImmunityAndNeverTouchesEnergy()
    {
        var target = Monster.Restore(BaseRestoreData("slip-target", attack: 0, defense: 0));
        target.CcImmuneUntilTurn = 999;
        target.Energy = 77;

        KnockdownResolver.ApplyEnvironmentalFall(target);

        Check("An environmental fall (Water/Ice slip) sets the actor prone even under crowd-control immunity -- it isn't an ability-induced knockdown",
            target.IsProne);
        Check("An environmental fall never touches Energy -- the move onto the tile is already this turn's spent action",
            target.Energy == 77);
    }

    private static void ConsumeReadyActionIfBankedOnlyRemovesEnergyWhenAnActionIsActuallyBanked()
    {
        var scheduler = new TurnScheduler();
        var belowThreshold = Monster.Restore(BaseRestoreData("below-threshold", attack: 0, defense: 0));
        belowThreshold.Energy = 40;

        scheduler.ConsumeReadyActionIfBanked(belowThreshold);

        Check("An actor with less than a full ready action banked is left completely untouched",
            belowThreshold.Energy == 40);
    }

    private static void StandClearsProneAndConsumesATurn()
    {
        var player = new Player("StandTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(101)));
        var gameLoop = new GameLoop(player);
        player.IsProne = true;

        bool turnConsumed = gameLoop.HandleStand();

        Check("Stand clears IsProne", !player.IsProne);
        Check("Stand always consumes a turn", turnConsumed);
        Check("Stand reports getting back on your feet", gameLoop.StatusMessages[^1] == "You get back on your feet.");
    }

    private static void StandWhileAlreadyStandingIsFreeAndReportsSo()
    {
        var player = new Player("AlreadyStandingTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(102)));
        var gameLoop = new GameLoop(player);

        bool turnConsumed = gameLoop.HandleStand();

        Check("Standing while not prone consumes no turn", !turnConsumed);
        Check("Standing while not prone reports the correct message", gameLoop.StatusMessages[^1] == "You are already standing.");
    }

    private static void IsAllowedWhileProneAllowsOnlyInformationalCommandsAndRejectsEverythingElse()
    {
        var allowed = new[]
        {
            PlayerCommand.None, PlayerCommand.Quit, PlayerCommand.ShowHelp, PlayerCommand.OpenInventory,
            PlayerCommand.ShowMessageHistory, PlayerCommand.ShowAdventureRecord, PlayerCommand.Look, PlayerCommand.LookHere
        };
        Check("Every informational/read-only command remains usable while prone",
            allowed.All(GameLoop.IsAllowedWhileProne));

        var rejected = new[]
        {
            PlayerCommand.Wait, PlayerCommand.PickUp, PlayerCommand.DropItem, PlayerCommand.Push, PlayerCommand.LearnSpell,
            PlayerCommand.MoveNorth, PlayerCommand.CastSpell, PlayerCommand.UseSkill, PlayerCommand.OpenChest,
            PlayerCommand.FireProjectile, PlayerCommand.AscendStairs, PlayerCommand.DescendStairs, PlayerCommand.Search,
            PlayerCommand.Sleep, PlayerCommand.Stand, PlayerCommand.SwapWithPet
        };
        Check("Every other command -- including the ones the proposal never explicitly names (Wait/Pick Up/Drop Item/Push/Learn Spell) -- is rejected by the allowlist while prone",
            rejected.All(c => !GameLoop.IsAllowedWhileProne(c)));
    }

    private static void ResolveNonPlayerTurnAutoStandsAProneMonsterInsteadOfRunningAi()
    {
        var level = BuildOpenLevel(3, 3);
        var monster = Monster.Restore(BaseRestoreData("prone-monster", attack: 0, defense: 0));
        monster.X = 1;
        monster.Y = 1;
        monster.IsProne = true;
        level.Actors.Add(monster);

        string message = GameLoop.ResolveNonPlayerTurn(monster, level, new Random(103));

        Check("A prone monster's turn clears IsProne instead of running its AI", !monster.IsProne);
        Check("The turn reports getting back on its feet", message.Contains("gets back on its feet"));
    }

    private static void ResolveNonPlayerTurnPrioritizesStunOverProne()
    {
        var level = BuildOpenLevel(3, 3);
        var monster = Monster.Restore(BaseRestoreData("stunned-and-prone-monster", attack: 0, defense: 0));
        monster.X = 1;
        monster.Y = 1;
        monster.IsProne = true;
        monster.StunnedUntilTurn = 100;
        level.Actors.Add(monster);

        string message = GameLoop.ResolveNonPlayerTurn(monster, level, new Random(104));

        Check("A monster that's both stunned and prone reports the stun message this turn, not the stand message",
            message.Contains("is stunned and cannot act"));
        Check("Being stunned this turn doesn't clear the pending prone state -- it still needs to stand once the stun expires",
            monster.IsProne);
    }

    private static void BashKnocksDownATargetItHits()
    {
        var (player, target, level) = BuildBashTestScenario();
        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.Shield);

        var result = CastBash(player, target, level, new DeterministicCombatRandom());

        Check("A landed Bash knocks its target prone", result.Success && target.IsProne);
    }

    private static void BashDoesNotKnockDownATargetItMisses()
    {
        var (player, target, level) = BuildBashTestScenario();
        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.Shield);

        var result = CastBash(player, target, level, new ForcedMissRandom());

        Check("A missed Bash never knocks its target down", result.AttackHit == false && !target.IsProne);
    }

    private static void BashNoLongerAppliesTheOldStun()
    {
        var (player, target, level) = BuildBashTestScenario();
        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.Shield);

        var result = CastBash(player, target, level, new DeterministicCombatRandom());

        Check("A landed Bash never sets StunnedUntilTurn -- Stun was replaced by Knockdown, not added alongside it",
            result.Success && target.StunnedUntilTurn == 0);
    }

    private static void TripIsGrantedOnlyToTheThiefAtLevelSeven()
    {
        Check("Trip's allowed classes are exactly Thief",
            SkillCatalog.Trip.AllowedClasses.Count == 1 && SkillCatalog.Trip.AllowedClasses.Contains(CharacterClass.Thief));
        Check("Trip unlocks at level 7", SkillCatalog.Trip.Level == 7);
    }

    private static void TripHasNoWeaponAttackRoll()
    {
        Check("Trip's effect list contains no WeaponDamageEffect -- its success is a pure proficiency-scaled roll, not an attack roll",
            !SkillCatalog.Trip.Effects.OfType<WeaponDamageEffect>().Any());
    }

    /// <summary>Fresh Thief + 0-defense target/level for each Trip test -- mirrors BuildBashTestScenario.</summary>
    private static (Player Player, Monster Target, Level Level) BuildTripTestScenario()
    {
        var level = BuildOpenLevel(3, 3);
        var player = new Player("TripTester", CharacterClass.Thief, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Thief, new Random(105)))
        {
            X = 1,
            Y = 1
        };
        level.Actors.Add(player);

        var target = Monster.Restore(BaseRestoreData("trip-target", attack: 0, defense: 0));
        target.X = 2;
        target.Y = 1;
        level.Actors.Add(target);

        return (player, target, level);
    }

    private static SpellCastResult CastTrip(Player player, Monster target, Level level, Random rng, RankScaling rankScaling = null) =>
        SkillCaster.Cast(SkillCatalog.Trip, new SpellCastingContext(player, level, level.TurnNumber, rng) { TargetActor = target, RankScaling = rankScaling ?? RankScaling.Neutral });

    private static void TripKnocksDownWithoutDealingDamage()
    {
        var (player, target, level) = BuildTripTestScenario();

        var result = CastTrip(player, target, level, new DeterministicCombatRandom());

        Check("A successful Trip knocks the target down", result.Success && target.IsProne);
        Check("Trip deals no damage at all -- the knockdown is the entire effect", result.DamageDealt == 0);
    }

    private static void TripStillConsumesCooldownOnAFailedRoll()
    {
        var (player, target, level) = BuildTripTestScenario();

        // Novice's 75% application chance, forced to fail with a roll well above it.
        var result = CastTrip(player, target, level, new ForcedMissRandom(), new RankScaling(ProficiencyRank.Novice));

        Check("A failed Trip roll still resolves as a successful cast -- the roll failing isn't a cast failure", result.Success);
        Check("The target is never knocked down when the roll fails", !target.IsProne);
        Check("Trip's cooldown is still consumed on a failed roll", player.SkillCooldowns.ContainsKey(SkillCatalog.Trip));
    }

    private static void TremorIsOnlyAvailableToMage()
    {
        Check("Tremor's allowed classes are exactly Mage",
            SpellCatalog.Tremor.AllowedClasses.Count == 1 && SpellCatalog.Tremor.AllowedClasses.Contains(CharacterClass.Mage));
        Check("Tremor is not a shared AllSpellcasters spell", !SpellCatalog.Tremor.AllSpellcasters);
    }

    /// <summary>Fresh Mage + 0-defense target/level for each Tremor test -- mirrors BuildBashTestScenario.</summary>
    private static (Player Player, Monster Target, Level Level) BuildTremorTestScenario()
    {
        var level = BuildOpenLevel(3, 3);
        var player = new Player("TremorTester", CharacterClass.Mage, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Mage, new Random(106)))
        {
            X = 1,
            Y = 1
        };
        player.Mana.SetCurrent(50);
        level.Actors.Add(player);

        var target = Monster.Restore(BaseRestoreData("tremor-target", attack: 0, defense: 0));
        target.X = 2;
        target.Y = 1;
        level.Actors.Add(target);

        return (player, target, level);
    }

    private static SpellCastResult CastTremor(Player player, Monster target, Level level, Random rng, RankScaling rankScaling = null) =>
        SpellCaster.Cast(SpellCatalog.Tremor, new SpellCastingContext(player, level, level.TurnNumber, rng) { TargetActor = target, RankScaling = rankScaling ?? RankScaling.Neutral });

    private static void TremorDealsNoDamageAndKnocksDownAtRange()
    {
        var (player, target, level) = BuildTremorTestScenario();

        var result = CastTremor(player, target, level, new DeterministicCombatRandom());

        Check("A successful Tremor knocks the target down", result.Success && target.IsProne);
        Check("Tremor deals no damage", result.DamageDealt == 0);
        Check("Tremor consumes its 5 mana", result.ManaConsumed == 5);
        Check("Tremor reports its earthquake-flavored success message",
            result.FlavorMessages.Any(m => m.Contains("thrown from their feet")));
    }

    private static void TremorFailedRollLeavesTargetStandingWithFlavorMessage()
    {
        var (player, target, level) = BuildTremorTestScenario();

        var result = CastTremor(player, target, level, new ForcedMissRandom(), new RankScaling(ProficiencyRank.Novice));

        Check("A resisted/failed Tremor still resolves as a cast but leaves the target standing",
            result.Success && !target.IsProne);
        Check("Tremor reports its earthquake-flavored failure message",
            result.FlavorMessages.Any(m => m.Contains("keeps its footing")));
    }

    private static void SlipChanceFormulaMatchesWorkedExamples()
    {
        Check("Ice slip chance at Luck 4 with footwear matches the proposal's own worked example (2.6%)",
            Math.Abs(SlipConfig.SlipChance(FloorType.Ice, feetSlotOccupied: true, adjustedLuck: 4) - 0.026) < 0.0001);
        Check("Ice slip chance at Luck 10 with footwear matches the proposal's own worked example (2.0%)",
            Math.Abs(SlipConfig.SlipChance(FloorType.Ice, feetSlotOccupied: true, adjustedLuck: 10) - 0.02) < 0.0001);
        Check("Ice slip chance at Luck 18 with footwear matches the proposal's own worked example (1.2%)",
            Math.Abs(SlipConfig.SlipChance(FloorType.Ice, feetSlotOccupied: true, adjustedLuck: 18) - 0.012) < 0.0001);
    }

    private static void BarefootDoublesTheSlipChance()
    {
        double shod = SlipConfig.SlipChance(FloorType.Ice, feetSlotOccupied: true, adjustedLuck: 10);
        double barefoot = SlipConfig.SlipChance(FloorType.Ice, feetSlotOccupied: false, adjustedLuck: 10);

        Check("A barefoot character's slip chance is exactly double the shod chance, all else equal",
            Math.Abs(barefoot - shod * 2) < 0.0001);
    }

    private static void NormalFloorNeverTriggersASlipRoll()
    {
        Check("Normal floor has a zero base slip chance", SlipConfig.BaseChanceFor(FloorType.Normal) == 0.0);
        Check("Normal floor's computed slip chance is zero regardless of footwear/luck",
            SlipConfig.SlipChance(FloorType.Normal, feetSlotOccupied: false, adjustedLuck: 1) == 0.0);
    }

    private static void WaterUsesALowerBaseChanceThanIce()
    {
        Check("Water's base slip chance (0.5%) is lower than Ice's (2%)",
            SlipConfig.BaseChanceFor(FloorType.Water) < SlipConfig.BaseChanceFor(FloorType.Ice));
    }

    private static void SlipChanceLuckModifierStaysWithinItsConfiguredClampBounds()
    {
        Check("An extremely high Luck still clamps the modifier at its floor (0.5x)",
            Math.Abs(SlipConfig.SlipChance(FloorType.Ice, feetSlotOccupied: true, adjustedLuck: 999) - SlipConfig.IceBaseChance * 0.5) < 0.0001);
        Check("An extremely low (or negative) Luck still clamps the modifier at its ceiling (1.5x)",
            Math.Abs(SlipConfig.SlipChance(FloorType.Ice, feetSlotOccupied: true, adjustedLuck: -999) - SlipConfig.IceBaseChance * 1.5) < 0.0001);
    }

    private static void RepeatedlyMovingOntoIceCanKnockThePlayerProneViaHandleMove()
    {
        var player = new Player("SlipTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(107)));
        player.Stats.Get(PrimaryAttribute.Luck).BaseValue = 1; // worst-case Luck -> highest slip chance
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;

        int iceX = player.X + 1, iceY = player.Y;
        level.Tiles[iceX, iceY] = Tile.CreateFloor();
        level.Tiles[iceX, iceY].FloorType = FloorType.Ice;
        level.Actors.RemoveAll(a => a.X == iceX && a.Y == iceY && a is not Player);
        // The randomly generated dungeon could otherwise have placed a door, chest, or blocking
        // room object at this exact relative offset, which would make every HandleMove attempt
        // bump/interact instead of ever actually stepping onto the ice tile at all -- never
        // rolling the slip chance this test exists to exercise.
        level.Doors.RemoveAll(d => d.X == iceX && d.Y == iceY);
        level.Chests.RemoveAll(c => c.X == iceX && c.Y == iceY);
        level.RoomObjects.RemoveAll(o => o.X == iceX && o.Y == iceY);

        bool everSlipped = false;
        for (int i = 0; i < 3000 && !everSlipped; i++)
        {
            player.X = iceX - 1;
            player.Y = iceY;
            player.IsProne = false;
            gameLoop.HandleMove(level, PlayerCommand.MoveEast);
            everSlipped = player.IsProne;
        }

        Check("Repeatedly moving onto Ice (worst-case Luck) eventually knocks the player prone via HandleMove",
            everSlipped);
    }

    private static void MovingOntoNormalFloorNeverTriggersASlipRegardlessOfIterationCount()
    {
        var player = new Player("NoSlipTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(108)));
        player.Stats.Get(PrimaryAttribute.Luck).BaseValue = 1;
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;

        int floorX = player.X + 1, floorY = player.Y;
        level.Tiles[floorX, floorY] = Tile.CreateFloor(); // FloorType.Normal by default
        level.Actors.RemoveAll(a => a.X == floorX && a.Y == floorY && a is not Player);

        for (int i = 0; i < 200; i++)
        {
            player.X = floorX - 1;
            player.Y = floorY;
            player.IsProne = false;
            gameLoop.HandleMove(level, PlayerCommand.MoveEast);
        }

        Check("Moving onto ordinary floor repeatedly never triggers a slip", !player.IsProne);
    }

    private static void PlayerIsProneSurvivesASaveLoadRoundTrip()
    {
        var data = new SaveData
        {
            ClassName = CharacterClass.Warrior.Name,
            RaceName = Race.Human.Name,
            Level = 1,
            Stats = BuildFullStatsData(),
            PlayerIsProne = true
        };

        var restored = SaveManager.ToPlayer(data);

        Check("PlayerIsProne survives a save/load round trip", restored.IsProne);
    }

    private static void MonsterRestoreDataAppliesIsProne()
    {
        var data = BaseRestoreData("restore-prone-monster", attack: 0, defense: 0);
        data.IsProne = true;

        var monster = Monster.Restore(data);

        Check("A monster restored from RestoreData with IsProne=true comes back prone", monster.IsProne);
    }

    private static void LookIndicatesWhenATargetIsProne()
    {
        var rng = new Random(109);
        var player = new Player("LookProneTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, rng));
        var monster = Monster.CreateRandom(0, 0, difficultyLevel: 1, rng);
        monster.IsProne = true;

        string description = LookService.DescribeCharacter(player, monster);

        Check("Looking at a prone monster shows a (Prone) indicator", description.Contains("(Prone)"));
    }

    // --- New Priest Skill Progression -------------------------------------------------------

    /// <summary>Fresh level-1 Priest + a 0-attack/0-defense target/level, mirroring BuildBashTestScenario -- shared by every new Priest skill test below.</summary>
    private static (Player Player, Monster Target, Level Level) BuildPriestTestScenario(CreatureType targetType = CreatureType.Other)
    {
        var level = BuildOpenLevel(5, 5);
        var player = new Player("PriestTester", CharacterClass.Priest, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Priest, new Random(201)))
        {
            X = 2,
            Y = 2
        };
        level.Actors.Add(player);

        var target = Monster.Restore(BaseRestoreData("priest-target", attack: 0, defense: 0, type: targetType));
        target.X = 3;
        target.Y = 2;
        level.Actors.Add(target);

        return (player, target, level);
    }

    private static SpellCastResult CastPriestSkill(Skill skill, Player player, Level level, Random rng, Actor target = null, RankScaling rankScaling = null) =>
        SkillCaster.Cast(skill, new SpellCastingContext(player, level, level.TurnNumber, rng) { TargetActor = target, RankScaling = rankScaling ?? RankScaling.Neutral });

    // --- Progression and persistence ---

    private static void EachNewPriestSkillIsGrantedAtItsIntendedLevelNotEarlier()
    {
        var expectations = new (Skill Skill, int Level)[]
        {
            (SkillCatalog.LayOnHands, 3), (SkillCatalog.TurnUndead, 6), (SkillCatalog.Purify, 10),
            (SkillCatalog.Steadfast, 14), (SkillCatalog.Exorcism, 19), (SkillCatalog.Intercession, 25)
        };

        foreach (var (skill, level) in expectations)
        {
            var priest = new Player("ProgressionTester", CharacterClass.Priest, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Priest, new Random(202)));
            for (int l = 2; l < level; l++)
            {
                priest.GrantSkillsForLevel(l);
            }
            Check($"{skill.Name} is not yet known one level below its intended grant level ({level - 1})", !priest.KnownSkills.Contains(skill));

            priest.GrantSkillsForLevel(level);
            Check($"{skill.Name} is granted exactly at level {level}", priest.KnownSkills.Contains(skill));
        }
    }

    private static void NoneOfTheSixNewPriestSkillsIsGrantedToAnyOtherClass()
    {
        var newSkills = new[] { SkillCatalog.LayOnHands, SkillCatalog.TurnUndead, SkillCatalog.Purify, SkillCatalog.Steadfast, SkillCatalog.Exorcism, SkillCatalog.Intercession };
        foreach (var skill in newSkills)
        {
            Check($"{skill.Name}'s allowed classes are exactly [Priest]",
                skill.AllowedClasses.Count == 1 && skill.AllowedClasses.Contains(CharacterClass.Priest));
        }
    }

    private static void PriestBashRemainsUnchangedAtLevelFive()
    {
        Check("Bash's Priest unlock level is still exactly 5, untouched by this proposal",
            SkillCatalog.Bash.LevelFor(CharacterClass.Priest) == 5);
    }

    private static void PlayerIntercessionStateSurvivesASaveLoadRoundTrip()
    {
        var data = new SaveData
        {
            ClassName = CharacterClass.Priest.Name,
            RaceName = Race.Human.Name,
            Level = 25,
            Stats = BuildFullStatsData(),
            PlayerIntercessionActive = true,
            PlayerIntercessionExpiresOnTurn = 500
        };

        var restored = SaveManager.ToPlayer(data);

        Check("IntercessionActive survives a save/load round trip", restored.IntercessionActive);
        Check("IntercessionExpiresOnTurn survives a save/load round trip", restored.IntercessionExpiresOnTurn == 500);
    }

    private static void MonsterFrightenedUntilTurnSurvivesRestoreData()
    {
        var data = BaseRestoreData("restore-frightened-monster", attack: 0, defense: 0);
        data.FrightenedUntilTurn = 77;

        var monster = Monster.Restore(data);

        Check("A monster restored from RestoreData with a FrightenedUntilTurn value comes back frightened until that turn",
            monster.FrightenedUntilTurn == 77);
    }

    // --- Lay on Hands ---

    private static void LayOnHandsHealsTenPlusAdjustedWisdomBeforeProficiencyScaling()
    {
        var (player, _, level) = BuildPriestTestScenario();
        player.Health.TakeDamage(100);
        player.Stats.Get(PrimaryAttribute.Wisdom).BaseValue = 8;
        player.Stats.Get(PrimaryAttribute.Wisdom).ClassModifier = 0;
        player.Stats.Get(PrimaryAttribute.Wisdom).EquipmentModifier = 0;
        player.Stats.Get(PrimaryAttribute.Wisdom).OtherModifier = 0;

        var result = CastPriestSkill(SkillCatalog.LayOnHands, player, level, new Random(1));

        Check("Lay on Hands heals exactly 10 + adjusted Wisdom (10 + 8 = 18) at Proficient rank (no scaling)",
            result.Success && result.HealingDone == 18);
    }

    private static void LayOnHandsNeverExceedsMaximumHealth()
    {
        var (player, _, level) = BuildPriestTestScenario();
        player.Health.TakeDamage(1); // 1 HP missing -- far less than any Lay on Hands roll

        CastPriestSkill(SkillCatalog.LayOnHands, player, level, new Random(1));

        Check("Health never exceeds Max after Lay on Hands overheals", player.Health.Current == player.Health.Max);
    }

    private static void LayOnHandsAtFullHealthIsRejectedWithoutConsumingATurnOrCooldown()
    {
        var (player, _, level) = BuildPriestTestScenario();

        var result = CastPriestSkill(SkillCatalog.LayOnHands, player, level, new Random(1));

        Check("Lay on Hands at full health fails outright", !result.Success && result.FailureReason == "You have no wounds to mend.");
        Check("A rejected Lay on Hands never starts its cooldown", !player.SkillCooldowns.ContainsKey(SkillCatalog.LayOnHands));
    }

    private static void LayOnHandsWorksWhileSilenced()
    {
        var (player, _, level) = BuildPriestTestScenario();
        player.Health.TakeDamage(50);
        player.SilencedUntilTurn = level.TurnNumber + 10;

        var result = CastPriestSkill(SkillCatalog.LayOnHands, player, level, new Random(1));

        Check("Lay on Hands succeeds while Silenced -- it's a skill, not a spell", result.Success && result.HealingDone > 0);
    }

    private static void LayOnHandsConsumesItsCooldownOnSuccess()
    {
        var (player, _, level) = BuildPriestTestScenario();
        player.Health.TakeDamage(50);

        CastPriestSkill(SkillCatalog.LayOnHands, player, level, new Random(1));

        Check("A successful Lay on Hands starts its 12-turn cooldown",
            player.SkillCooldowns.TryGetValue(SkillCatalog.LayOnHands, out int readyTurn) && readyTurn == level.TurnNumber + 12);
    }

    // --- Turn Undead ---

    private static void TurnUndeadOnlyAffectsVisibleUndeadWithinRadius()
    {
        var level = BuildOpenLevel(11, 11);
        var player = new Player("TurnUndeadTester", CharacterClass.Priest, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Priest, new Random(203))) { X = 5, Y = 5 };
        level.Actors.Add(player);

        var nearUndead = Monster.Restore(BaseRestoreData("near-undead", attack: 0, defense: 0, type: CreatureType.Undead));
        nearUndead.X = 6; nearUndead.Y = 5; // distance 1 -- within radius 4
        level.Actors.Add(nearUndead);

        var farUndead = Monster.Restore(BaseRestoreData("far-undead", attack: 0, defense: 0, type: CreatureType.Undead));
        farUndead.X = 10; farUndead.Y = 5; // distance 5 -- outside radius 4
        level.Actors.Add(farUndead);

        var nearLiving = Monster.Restore(BaseRestoreData("near-living", attack: 0, defense: 0, type: CreatureType.Animal));
        nearLiving.X = 5; nearLiving.Y = 6; // distance 1, but not Undead
        level.Actors.Add(nearLiving);

        var context = new SpellCastingContext(player, level, level.TurnNumber, new Random(1));
        TargetResolver.ResolveAffectedActors(SkillCatalog.TurnUndead.Targeting, context, out _);

        Check("Only the near, in-radius Undead is eligible", context.AffectedActors.Count == 1 && context.AffectedActors.Contains(nearUndead));
        Check("An out-of-radius Undead is excluded", !context.AffectedActors.Contains(farUndead));
        Check("A non-Undead creature at the same distance is excluded", !context.AffectedActors.Contains(nearLiving));
    }

    private static void TurnUndeadEachTargetResolvesIndependentlyAndSkipsNonUndead()
    {
        var (player, target, level) = BuildPriestTestScenario(CreatureType.Undead);

        var result = CastPriestSkill(SkillCatalog.TurnUndead, player, level, new DeterministicCombatRandom());

        Check("A successful Turn Undead frightens the Undead target", result.Success && level.TurnNumber < target.FrightenedUntilTurn);
    }

    private static void TurnUndeadDoesNotAffectLivingCreatures()
    {
        var (player, target, level) = BuildPriestTestScenario(CreatureType.Animal);

        CastPriestSkill(SkillCatalog.TurnUndead, player, level, new DeterministicCombatRandom());

        Check("A living creature within radius is never frightened by Turn Undead", target.FrightenedUntilTurn == 0);
    }

    private static void TurnUndeadBossResistancePenaltyReducesTheChanceWithoutEliminatingIt()
    {
        var (player, boss, level) = BuildPriestTestScenario(CreatureType.Undead);
        boss.IsBoss = true;

        // Proficient rank's chance is 100%; the boss penalty drops it to 70%. A roll of 0.8 fails
        // against the boss (>= 0.7) but would have succeeded against a non-boss (< 1.0).
        var rng = new FixedRollRandom(0.8);
        var result = CastPriestSkill(SkillCatalog.TurnUndead, player, level, rng);

        Check("A roll that would succeed at 100% fails against an undead boss once the 30-point penalty applies",
            result.Success && boss.FrightenedUntilTurn == 0);
    }

    private static void TurnUndeadWithNoEligibleTargetsIsRejectedWithoutConsumingATurnOrCooldown()
    {
        var level = BuildOpenLevel(5, 5);
        var player = new Player("NoUndeadTester", CharacterClass.Priest, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Priest, new Random(204))) { X = 2, Y = 2 };
        level.Actors.Add(player);

        var result = CastPriestSkill(SkillCatalog.TurnUndead, player, level, new Random(1));

        Check("Turn Undead with no undead nearby fails outright", !result.Success && result.FailureReason == "There are no undead nearby to turn.");
        Check("A rejected Turn Undead never starts its cooldown", !player.SkillCooldowns.ContainsKey(SkillCatalog.TurnUndead));
    }

    private static void TurnUndeadStartsCooldownEvenWhenEveryTargetResists()
    {
        var (player, target, level) = BuildPriestTestScenario(CreatureType.Undead);

        // Proficient rank's 100% chance can never actually fail against ForcedMissRandom's 0.999
        // roll (0.999 < 1.0 still "succeeds") -- Novice's 75% chance is beatable.
        CastPriestSkill(SkillCatalog.TurnUndead, player, level, new ForcedMissRandom(), rankScaling: new RankScaling(ProficiencyRank.Novice));

        Check("Turn Undead's cooldown starts even though the only target resisted",
            player.SkillCooldowns.ContainsKey(SkillCatalog.TurnUndead));
        Check("The resisting target was never actually frightened", target.FrightenedUntilTurn == 0);
    }

    // --- Frightened AI ---

    private static void FrightenedActorFleesAwayFromTheSourceInsteadOfActingNormally()
    {
        var level = BuildOpenLevel(11, 5);
        var player = new Player("FleeSourceTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(205))) { X = 5, Y = 2 };
        level.Actors.Add(player);

        var monster = Monster.Restore(BaseRestoreData("fleeing-undead", attack: 0, defense: 0, type: CreatureType.Undead));
        monster.X = 7; monster.Y = 2; // 2 tiles east of the player -- not adjacent
        monster.FrightenedUntilTurn = level.TurnNumber + 10;
        level.Actors.Add(monster);

        string message = GameLoop.ResolveNonPlayerTurn(monster, level, new Random(1));

        Check("A frightened, non-adjacent monster moves further away from the player rather than closer",
            monster.X > 7);
        Check("Fleeing reports a flee message", message != null && message.Contains("flees in terror"));
    }

    private static void FrightenedActorNeverEndsUpCloserToTheSource()
    {
        var level = BuildOpenLevel(11, 5);
        var player = new Player("NeverCloserTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(206))) { X = 5, Y = 2 };
        level.Actors.Add(player);

        var monster = Monster.Restore(BaseRestoreData("boxed-undead", attack: 0, defense: 0, type: CreatureType.Undead));
        monster.X = 6; monster.Y = 2;
        monster.FrightenedUntilTurn = level.TurnNumber + 10;
        level.Actors.Add(monster);

        int distanceBefore = Math.Abs(monster.X - player.X);
        GameLoop.ResolveNonPlayerTurn(monster, level, new Random(1));
        int distanceAfter = Math.Abs(monster.X - player.X);

        Check("A frightened monster's distance from the player never decreases", distanceAfter >= distanceBefore);
    }

    private static void FrightenedActorAdjacentToTheSourceStillActsInsteadOfFreezing()
    {
        var level = BuildOpenLevel(5, 5);
        var player = new Player("AdjacentFrightTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(207))) { X = 2, Y = 2 };
        level.Actors.Add(player);

        var monster = Monster.Restore(BaseRestoreData("cornered-undead", attack: 5, defense: 0, type: CreatureType.Undead));
        monster.X = 3; monster.Y = 2; // adjacent to the player
        monster.FrightenedUntilTurn = level.TurnNumber + 10;
        level.Actors.Add(monster);

        string message = GameLoop.ResolveNonPlayerTurn(monster, level, new DeterministicCombatRandom());

        Check("An adjacent frightened monster still attacks (defends itself) instead of doing nothing",
            message != null && message.Length > 0);
        Check("Attacking in place never moves the frightened monster", monster.X == 3 && monster.Y == 2);
    }

    // --- Purify ---

    private static void PurifyRemovesAHostileStatusEffectButNotASelfBuff()
    {
        var (player, _, level) = BuildPriestTestScenario();
        player.ActiveEffects.Add(new ActiveEffect("Poisoned", level.TurnNumber + 10) { TickDamage = 2, TickDamageType = DamageType.Poison, CanBePurified = true });
        player.ActiveEffects.Add(new ActiveEffect("Bless", level.TurnNumber + 10) { ModifiedStat = Stat.PhysicalAttack, StatAmount = 3, CanBePurified = false });

        var result = CastPriestSkill(SkillCatalog.Purify, player, level, new Random(1));

        Check("Purify succeeds", result.Success);
        Check("The hostile Poisoned status is removed", !player.ActiveEffects.Any(e => e.SourceSpellName == "Poisoned"));
        Check("The self-applied Bless buff is left untouched", player.ActiveEffects.Any(e => e.SourceSpellName == "Bless"));
    }

    private static void PurifyRemovesSilencedAndFrightenedButNotStunnedOrProne()
    {
        var (player, _, level) = BuildPriestTestScenario();
        player.SilencedUntilTurn = level.TurnNumber + 10;
        player.FrightenedUntilTurn = level.TurnNumber + 10;
        player.StunnedUntilTurn = level.TurnNumber + 10;
        player.IsProne = true;

        CastPriestSkill(SkillCatalog.Purify, player, level, new Random(1));

        Check("Purify clears Silenced", level.TurnNumber >= player.SilencedUntilTurn);
        Check("Purify clears Frightened", level.TurnNumber >= player.FrightenedUntilTurn);
        Check("Purify leaves Stunned untouched -- not a purifiable condition", level.TurnNumber < player.StunnedUntilTurn);
        Check("Purify leaves Prone untouched -- requires the Stand command instead", player.IsProne);
    }

    private static void PurifyWithNothingToRemoveIsRejectedWithoutConsumingATurnOrCooldown()
    {
        var (player, _, level) = BuildPriestTestScenario();

        var result = CastPriestSkill(SkillCatalog.Purify, player, level, new Random(1));

        Check("Purify with nothing to remove fails outright", !result.Success && result.FailureReason == "You have no affliction that Purify can remove.");
        Check("A rejected Purify never starts its cooldown", !player.SkillCooldowns.ContainsKey(SkillCatalog.Purify));
    }

    private static void PurifyRemovesMultipleEligibleConditionsInOneUse()
    {
        var (player, _, level) = BuildPriestTestScenario();
        player.ActiveEffects.Add(new ActiveEffect("Burning", level.TurnNumber + 10) { TickDamage = 2, TickDamageType = DamageType.Fire, CanBePurified = true });
        player.ActiveEffects.Add(new ActiveEffect("Crippling Strike", level.TurnNumber + 10) { ModifiedStat = Stat.MovementSpeed, StatAmount = -4, CanBePurified = true });
        player.SilencedUntilTurn = level.TurnNumber + 10;

        CastPriestSkill(SkillCatalog.Purify, player, level, new Random(1));

        Check("Every purifiable ActiveEffect is removed in one use", player.ActiveEffects.Count == 0);
        Check("Silenced is cleared in the same use", level.TurnNumber >= player.SilencedUntilTurn);
    }

    // --- Steadfast ---

    /// <summary>Rolls exactly at the 25% boundary -- 0.24999 resists, 0.25 does not (NextDouble() &lt; 0.25).</summary>
    private class FixedRollRandom : Random
    {
        private readonly double roll;
        public FixedRollRandom(double roll) => this.roll = roll;
        public override double NextDouble() => roll;
        public override int Next(int minValue, int maxValue) => minValue;
    }

    private static void SteadfastResistanceChanceIsExactlyTwentyFivePercent()
    {
        var priest = new Player("SteadfastTester", CharacterClass.Priest, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Priest, new Random(208)));
        priest.KnownSkills.Add(SkillCatalog.Steadfast);

        Check("A roll just under 0.25 resists", CrowdControlResolver.ResistedBySteadfast(priest, new FixedRollRandom(0.2499)));
        Check("A roll of exactly 0.25 does not resist", !CrowdControlResolver.ResistedBySteadfast(priest, new FixedRollRandom(0.25)));
    }

    private static void SteadfastCanResistKnockdownStunAndFrightened()
    {
        var priest = new Player("SteadfastAllThreeTester", CharacterClass.Priest, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Priest, new Random(209)));
        priest.KnownSkills.Add(SkillCatalog.Steadfast);
        var resistRoll = new FixedRollRandom(0.0);

        var knockdownOutcome = KnockdownResolver.TryApply(priest, turnNumber: 10, new TurnScheduler(), resistRoll);
        Check("Steadfast resists a knockdown", knockdownOutcome == KnockdownOutcome.ResistedBySteadfast && !priest.IsProne);

        var stunOutcome = CrowdControlResolver.TryApplyStun(priest, scaledDuration: 3, turnNumber: 10, resistRoll);
        Check("Steadfast resists a stun", stunOutcome == CrowdControlOutcome.ResistedBySteadfast && priest.StunnedUntilTurn == 0);

        var frightenOutcome = CrowdControlResolver.TryApplyFrightened(priest, scaledDuration: 3, turnNumber: 10, resistRoll);
        Check("Steadfast resists a fright", frightenOutcome == CrowdControlOutcome.ResistedBySteadfast && priest.FrightenedUntilTurn == 0);
    }

    private static void SteadfastNeverAppliesToAMonster()
    {
        var monster = Monster.Restore(BaseRestoreData("no-steadfast-monster", attack: 0, defense: 0));
        Check("A Monster (which can never know a Skill) is never resisted by Steadfast",
            !CrowdControlResolver.ResistedBySteadfast(monster, new FixedRollRandom(0.0)));
    }

    private static void SteadfastDoesNotResistAnEnvironmentalFall()
    {
        var priest = new Player("SteadfastVsSlipTester", CharacterClass.Priest, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Priest, new Random(210)));
        priest.KnownSkills.Add(SkillCatalog.Steadfast);

        KnockdownResolver.ApplyEnvironmentalFall(priest);

        Check("Steadfast never blocks a Water/Ice environmental fall -- ApplyEnvironmentalFall has no Steadfast check at all", priest.IsProne);
    }

    private static void CrowdControlImmunityTakesPriorityOverSteadfast()
    {
        var priest = new Player("ImmunityFirstTester", CharacterClass.Priest, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Priest, new Random(211)));
        priest.KnownSkills.Add(SkillCatalog.Steadfast);
        priest.CcImmuneUntilTurn = 50;

        var outcome = CrowdControlResolver.TryApplyStun(priest, scaledDuration: 3, turnNumber: 10, new FixedRollRandom(0.0));

        Check("Complete CC immunity reports Immune, not ResistedBySteadfast, when both would apply",
            outcome == CrowdControlOutcome.Immune);
    }

    // --- Exorcism ---

    private static void ExorcismRequiresAnAdjacentTarget()
    {
        var (player, target, level) = BuildPriestTestScenario(CreatureType.Undead);
        target.X = 4; target.Y = 2; // 2 tiles away -- out of Exorcism's range 1
        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.ShortSword);

        var result = CastPriestSkill(SkillCatalog.Exorcism, player, level, new DeterministicCombatRandom(), target);

        Check("Exorcism against a non-adjacent target fails outright", !result.Success);
    }

    private static void ExorcismRequiresAMeleeWeapon()
    {
        var (player, target, level) = BuildPriestTestScenario(CreatureType.Undead);

        var result = CastPriestSkill(SkillCatalog.Exorcism, player, level, new DeterministicCombatRandom(), target);

        Check("Exorcism with no melee weapon equipped fails outright", !result.Success && result.FailureReason == "Exorcism requires a melee weapon.");
        Check("A failed Exorcism attempt never starts its cooldown", !player.SkillCooldowns.ContainsKey(SkillCatalog.Exorcism));
    }

    private static void ExorcismRejectsANonUndeadTarget()
    {
        var (player, target, level) = BuildPriestTestScenario(CreatureType.Animal);
        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.ShortSword);

        var result = CastPriestSkill(SkillCatalog.Exorcism, player, level, new DeterministicCombatRandom(), target);

        Check("Exorcism against a non-Undead target fails outright", !result.Success && result.FailureReason == "Exorcism can only be used against the undead.");
    }

    private static void ExorcismMissApplesNoPhysicalOrHolyDamage()
    {
        var (player, target, level) = BuildPriestTestScenario(CreatureType.Undead);
        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.ShortSword);

        var result = CastPriestSkill(SkillCatalog.Exorcism, player, level, new ForcedMissRandom(), target);

        Check("A missed Exorcism deals no damage of any kind", result.Success && result.DamageDealt == 0);
    }

    private static void ExorcismHitAppliesBothPhysicalAndHolyDamage()
    {
        var (player, target, level) = BuildPriestTestScenario(CreatureType.Undead);
        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.ShortSword);
        int basePower = player.BasePhysicalAttackPower;

        var result = CastPriestSkill(SkillCatalog.Exorcism, player, level, new DeterministicCombatRandom(), target);

        // DeterministicCombatRandom's Next(min, max) always returns exactly 0 regardless of the
        // requested range, so the 2d6 Holy roll (DiceRoll.Roll calling rng.Next(1, 7) twice) comes
        // back as exactly 0 -- there is no resistance mitigation either way (Holy has no
        // ResistanceType mapping today -- see ResistanceTypeMapping.ForDamageType), so the total
        // is just the ordinary physical hit.
        Check("A landed Exorcism applies the normal physical hit, plus a (possibly zero) resistance-mitigated Holy bonus on top",
            result.Success && result.DamageDealt == basePower);
    }

    /// <summary>Forces every Next(min, max) call to its range minimum (rather than always 0, like DeterministicCombatRandom) -- unlike that class, this makes the 2d6 Holy dice roll come back as a real, non-zero, exactly-predictable value (1+1=2) instead of always 0.</summary>
    private class MinRollRandom : Random
    {
        public override double NextDouble() => 0.0;
        public override int Next(int minValue, int maxValue) => minValue;
    }

    private static void ExorcismHitAppliesANonZeroHolyBonusOnTopOfThePhysicalHit()
    {
        var (player, target, level) = BuildPriestTestScenario(CreatureType.Undead);
        player.EquipInSlot(EquipmentSlot.PrimaryHand, Items.ShortSword);
        int basePower = player.BasePhysicalAttackPower;

        var result = CastPriestSkill(SkillCatalog.Exorcism, player, level, new MinRollRandom(), target);

        // -1 damage variance (Next always returns the range minimum, -1 here) then +2 from the
        // minimum possible 2d6 Holy roll (each die forced to 1) -- net +1 over a plain basePower hit.
        Check("A landed Exorcism's total damage includes the non-zero Holy bonus on top of the physical hit",
            result.Success && result.DamageDealt == basePower + 1);
    }

    // --- Intercession ---

    private static void IntercessionPreventsLethalDamageLeavingExactlyOneHp()
    {
        var (player, _, _) = BuildPriestTestScenario();
        player.IntercessionActive = true;

        int actual = CombatStatsTracker.ApplyDamage(player, player.Health.Current + 50, null);

        Check("The player survives at exactly 1 HP", player.Health.Current == 1);
        Check("Intercession is consumed by triggering", !player.IntercessionActive);
        Check("A prominent activation message is queued", player.PendingInterceptionMessage != null && player.PendingInterceptionMessage.Contains("Divine light"));
        Check("The reported actual damage matches the amount actually prevented down to 1 HP", actual > 0);
    }

    private static void IntercessionDoesNotTriggerOnNonLethalDamage()
    {
        var (player, _, _) = BuildPriestTestScenario();
        player.IntercessionActive = true;
        int healthBefore = player.Health.Current;

        CombatStatsTracker.ApplyDamage(player, 1, null);

        Check("Intercession stays active through non-lethal damage", player.IntercessionActive);
        Check("Non-lethal damage is applied completely normally", player.Health.Current == healthBefore - 1);
    }

    private static void LaterLethalDamageAfterInterceptionIsConsumedKillsNormally()
    {
        var (player, _, _) = BuildPriestTestScenario();
        player.IntercessionActive = true;

        CombatStatsTracker.ApplyDamage(player, player.Health.Current + 50, null); // consumes Intercession, leaves 1 HP
        CombatStatsTracker.ApplyDamage(player, 50, null); // a second lethal hit with no protection left

        Check("A second lethal hit after Intercession is consumed kills the player normally", player.Health.Current == 0);
    }

    private static void IntercessionExpiresAfterItsDurationWithNoLethalDamage()
    {
        var (player, _, level) = BuildPriestTestScenario();
        player.IntercessionActive = true;
        player.IntercessionExpiresOnTurn = level.TurnNumber + 1;

        // Mirrors GameLoop.Run's own per-completed-turn expiry check.
        level.AdvanceTurn();
        if (player.IntercessionActive && level.TurnNumber >= player.IntercessionExpiresOnTurn)
        {
            player.IntercessionActive = false;
        }

        Check("Intercession's protection expires harmlessly once its duration runs out", !player.IntercessionActive);
    }

    private static void RecastingIntercessionWhileActiveIsRejectedWithoutConsumingATurnOrCooldown()
    {
        var (player, _, level) = BuildPriestTestScenario();
        player.IntercessionActive = true;

        var result = CastPriestSkill(SkillCatalog.Intercession, player, level, new Random(1));

        Check("Recasting Intercession while already active fails outright", !result.Success && result.FailureReason == "Intercession already guards your life.");
        Check("A rejected recast never starts its cooldown", !player.SkillCooldowns.ContainsKey(SkillCatalog.Intercession));
    }

    private static void IntercessionActivatesWithTheCorrectDurationAndFlavorMessage()
    {
        var (player, _, level) = BuildPriestTestScenario();

        var result = CastPriestSkill(SkillCatalog.Intercession, player, level, new Random(1));

        Check("Intercession activates on cast", result.Success && player.IntercessionActive);
        Check("Intercession's window is exactly the 10-turn base duration at Proficient rank", player.IntercessionExpiresOnTurn == level.TurnNumber + 10);
        Check("Intercession's cast message is shown", result.Message.Contains("You entrust your life to divine protection."));
    }

    // --- Pet and Companion System -----------------------------------------------------------

    private static (Player Player, Pet Pet, Level Level) BuildPetTestScenario()
    {
        var level = BuildOpenLevel(10, 10);
        var player = new Player("PetOwnerTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(301)))
        {
            X = 5,
            Y = 5
        };
        level.Actors.Add(player);

        var pet = PetFactory.CreateDog(player);
        pet.MoveTo(5, 4);
        level.Actors.Add(pet);
        level.Scheduler.Register(pet);

        return (player, pet, level);
    }

    private static Monster PetTestMonster(string name, int x, int y, Level level, int hp = 1000, int attack = 0, int defense = 0)
    {
        var data = BaseRestoreData(name, attack, defense, CreatureType.Other);
        data.MaxHp = hp;
        data.CurrentHp = hp;
        var monster = Monster.Restore(data);
        monster.X = x;
        monster.Y = y;
        monster.AI = null; // these tests drive PetAI/GameLoop directly, never the monster's own turn
        level.Actors.Add(monster);
        return monster;
    }

    private static void PetProgressionStatsMatchTheDesignDocsWorkedTable()
    {
        Check("Level 1: 12 HP / 3 bite power", PetProgression.MaxHpFor(1) == 12 && PetProgression.BitePowerFor(1) == 3);
        Check("Level 2: 16 HP / 3 bite power", PetProgression.MaxHpFor(2) == 16 && PetProgression.BitePowerFor(2) == 3);
        Check("Level 3: 20 HP / 4 bite power", PetProgression.MaxHpFor(3) == 20 && PetProgression.BitePowerFor(3) == 4);
        Check("Level 4: 24 HP / 4 bite power", PetProgression.MaxHpFor(4) == 24 && PetProgression.BitePowerFor(4) == 4);
        Check("Level 5: 28 HP / 5 bite power", PetProgression.MaxHpFor(5) == 28 && PetProgression.BitePowerFor(5) == 5);
        Check("Level 10: 48 HP / 7 bite power", PetProgression.MaxHpFor(10) == 48 && PetProgression.BitePowerFor(10) == 7);
    }

    private static void SyncToLevelPreservesHealthPercentageAndGuaranteesAtLeastOneHeal()
    {
        var (_, pet, _) = BuildPetTestScenario();
        pet.Health.SetCurrent(6); // exactly half of the level-1 max (12)

        PetProgression.SyncToLevel(pet, ownerLevel: 2); // new max is 16

        Check("A level-up recomputes max HP from the new owner level", pet.Health.Max == 16);
        Check("Current HP scales to preserve the same percentage (half of 16 = 8), rounded in the pet's favor", pet.Health.Current == 8);

        pet.Health.SetCurrent(1);
        PetProgression.SyncToLevel(pet, ownerLevel: 3); // max grows again, from a nearly-empty health pool
        Check("A level-up always heals for at least 1 point", pet.Health.Current >= 1);
    }

    private static void OwnerLevelingUpResyncsThePetAutomatically()
    {
        var player = new Player("PetLevelUpTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(302)));
        var pet = PetFactory.CreateDog(player);

        player.AddExperience(LevelProgression.GetXpRequiredForNextLevel(player.Class, player.Level));

        Check("The pet's level matches the owner's after a level-up", pet.Level == player.Level);
        Check("The pet's max HP matches the new level's expected value", pet.Health.Max == PetProgression.MaxHpFor(player.Level));
        Check("A pending pet message announces the growth", player.PendingPetMessage != null);
    }

    private static void CreateDogProducesTheExpectedLevelOneIdentity()
    {
        var player = new Player("NewDogOwnerTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(303)));
        var pet = PetFactory.CreateDog(player);

        Check("The dog's display name is \"pet dog\"", pet.Name == "pet dog");
        Check("The dog bites", pet.AttackType == AttackType.Bite);
        Check("The dog starts at full health", pet.Health.Current == pet.Health.Max);
        Check("The dog starts at level 1 for a level-1 owner", pet.Level == 1);
        Check("The dog's owner is the player", ReferenceEquals(pet.Owner, player));
        Check("The player's Pet reference is set", ReferenceEquals(player.Pet, pet));
        Check("A freshly created dog is Active", pet.LifecycleState == PetLifecycleState.Active);
    }

    private static void AllegianceTreatsAnOwnerAndItsOwnPetAsFriendly()
    {
        var (player, pet, _) = BuildPetTestScenario();
        var monster = Monster.Restore(BaseRestoreData("unrelated-monster", attack: 0, defense: 0));

        Check("An owner and its own pet are friendly", Allegiance.AreFriendly(player, pet));
        Check("Friendliness is symmetric", Allegiance.AreFriendly(pet, player));
        Check("A pet and an unrelated monster are not friendly", !Allegiance.AreFriendly(pet, monster));
        Check("A player and an unrelated monster are not friendly", !Allegiance.AreFriendly(player, monster));
    }

    /// <summary>
    /// Forces a known 5x5 floor area, walled off on every side, with every stray actor the real
    /// (unseeded) dungeon generator happened to place inside it removed -- GameLoop's own
    /// CurrentLevel is a genuinely random dungeon, not a blank Level, so a bump-displacement test
    /// that only pins a couple of tiles can flake whenever generation happens to drop a monster
    /// or a non-floor tile onto one of the candidate sidestep spots. Player lands at (2,2), pet at
    /// (3,2); every tile from (1,1) to (3,3) is open floor, everything one ring further out is wall.
    /// </summary>
    private static void BuildIsolatedBumpTestArea(Level level)
    {
        for (int x = 0; x <= 4; x++)
        {
            for (int y = 0; y <= 4; y++)
            {
                level.Tiles[x, y] = (x >= 1 && x <= 3 && y >= 1 && y <= 3) ? Tile.CreateFloor() : Tile.CreateWall();
            }
        }
        level.Actors.RemoveAll(a => a.X >= 0 && a.X <= 4 && a.Y >= 0 && a.Y <= 4 && a is not Player and not Pet);
    }

    private static void BumpingIntoYourOwnPetNeverAttacksItAndDisplacesItInstead()
    {
        var player = new Player("BumpPetTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(304)));
        PetFactory.CreateDog(player);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        var pet = player.Pet;
        BuildIsolatedBumpTestArea(level);
        player.X = 2; player.Y = 2;
        pet.MoveTo(3, 2);
        int hpBefore = pet.Health.Current;

        bool turnConsumed = gameLoop.HandleMove(level, PlayerCommand.MoveEast);

        Check("Bumping into your own pet never damages it", pet.Health.Current == hpBefore);
        Check("Bumping into your own pet, when it has room to move, consumes a turn (the player actually steps through)", turnConsumed);
        Check("The player ends up on the pet's former tile", player.X == 3 && player.Y == 2);
        Check("The pet moved off of the player's new tile", pet.X != 3 || pet.Y != 2);
        Check("The pet didn't get shoved onto the player's OLD tile either", pet.X != 2 || pet.Y != 2);
    }

    private static void BumpingIntoAPetWithNoRoomToStepAsideFailsWithoutConsumingATurn()
    {
        var player = new Player("BumpBoxedPetTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(321)));
        PetFactory.CreateDog(player);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        var pet = player.Pet;
        BuildIsolatedBumpTestArea(level);
        // Re-wall every neighbor of the pet's tile except the player's own -- only (2,2) and
        // (3,2) stay open, so there's truly nowhere for the pet to step aside to.
        foreach (var (x, y) in new[] { (2, 1), (3, 1), (2, 3), (3, 3) })
        {
            level.Tiles[x, y] = Tile.CreateWall();
        }
        player.X = 2; player.Y = 2;
        pet.MoveTo(3, 2);

        bool turnConsumed = gameLoop.HandleMove(level, PlayerCommand.MoveEast);

        Check("Bumping into a completely boxed-in pet never consumes a turn", !turnConsumed);
        Check("...and the player stays put", player.X == 2 && player.Y == 2);
        Check("...and the pet stays put too", pet.X == 3 && pet.Y == 2);
    }

    private static void ADisplacedPetPrefersASafeTileOverAHazardousOne()
    {
        var player = new Player("BumpPetHazardTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(322)));
        PetFactory.CreateDog(player);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        var pet = player.Pet;
        BuildIsolatedBumpTestArea(level);
        // Every neighbor of the pet's tile (3,2) is Fire EXCEPT (3,1), the one safe option.
        foreach (var (x, y) in new[] { (3, 3), (2, 1), (2, 3) })
        {
            level.Tiles[x, y].FloorType = FloorType.Fire;
        }
        level.Tiles[3, 1].FloorType = FloorType.Normal;
        player.X = 2; player.Y = 2;
        pet.MoveTo(3, 2);

        gameLoop.HandleMove(level, PlayerCommand.MoveEast);

        Check("A displaced pet prefers the one safe tile over any of the surrounding Fire tiles", pet.X == 3 && pet.Y == 1);
    }

    private static void UKeyResolvesToTheSwapWithPetCommand()
    {
        var command = InputHandler.ResolveCommand(new ConsoleKeyInfo('u', ConsoleKey.U, shift: false, alt: false, control: false));
        Check("'U' resolves to SwapWithPet", command == PlayerCommand.SwapWithPet);
    }

    private static void SwappingWithAnAdjacentPetTradesTilesExactly()
    {
        var player = new Player("SwapPetTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(324)));
        PetFactory.CreateDog(player);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        var pet = player.Pet;
        BuildIsolatedBumpTestArea(level);
        player.X = 2; player.Y = 2;
        pet.MoveTo(3, 2);
        int hpBefore = pet.Health.Current;

        bool turnConsumed = gameLoop.HandleSwapWithPet(level);

        Check("Swapping with an adjacent pet consumes a turn", turnConsumed);
        Check("The player ends up exactly on the pet's former tile", player.X == 3 && player.Y == 2);
        Check("The pet ends up exactly on the player's former tile", pet.X == 2 && pet.Y == 2);
        Check("Swapping never damages the pet", pet.Health.Current == hpBefore);
    }

    private static void SwappingWithAPetTooFarAwayFailsWithoutConsumingATurn()
    {
        var player = new Player("FarSwapPetTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(326)));
        PetFactory.CreateDog(player);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        var pet = player.Pet;
        // The exact coordinate doesn't need to be walkable -- HandleSwapWithPet's distance check
        // runs (and rejects) before touching tiles/pathing at all.
        pet.MoveTo(player.X + 1000, player.Y);
        int petX = pet.X, petY = pet.Y;

        bool turnConsumed = gameLoop.HandleSwapWithPet(level);

        Check("Swapping with a pet that isn't adjacent fails without consuming a turn", !turnConsumed);
        Check("A failed swap leaves the pet exactly where it was", pet.X == petX && pet.Y == petY);
    }

    private static void SwappingWithNoPetAtAllFailsGracefully()
    {
        var player = new Player("NoPetSwapTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(325)));
        var gameLoop = new GameLoop(player); // never given a pet -- mirrors an old save predating this feature

        bool turnConsumed = gameLoop.HandleSwapWithPet(gameLoop.CurrentLevel);

        Check("Swapping with no pet at all fails cleanly instead of throwing", !turnConsumed);
    }

    private static void IdleRoamingPrefersSafeTilesOverHazardousOnesWhenPossible()
    {
        var (player, pet, level) = BuildPetTestScenario(); // player (5,5), pet (5,4)
        int homeX = pet.X, homeY = pet.Y;
        // Every neighbor of the pet's home tile is Fire except (homeX, homeY+1), which stays Normal.
        foreach (var (dx, dy) in new[] { (-1, -1), (0, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (1, 1) })
        {
            level.Tiles[homeX + dx, homeY + dy].FloorType = FloorType.Fire;
        }

        for (int seed = 0; seed < 100; seed++)
        {
            pet.MoveTo(homeX, homeY);
            new PetAI().TakeTurn(pet, level, new Random(seed));
            if (level.Tiles[pet.X, pet.Y].FloorType == FloorType.Fire)
            {
                Check("Idle roaming never steps onto a hazardous tile when a safe alternative exists", false);
                return;
            }
        }
        Check("Idle roaming never steps onto a hazardous tile when a safe alternative exists", true);
    }

    private static void ReturningToOwnerPrefersASafeStepOverAHazardousDiagonalOne()
    {
        var level = BuildOpenLevel(10, 10);
        var player = new Player("PetHazardReturnTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(323))) { X = 5, Y = 3 };
        level.Actors.Add(player);
        var pet = PetFactory.CreateDog(player);
        pet.MoveTo(0, 0); // well outside the roaming area -- must return
        level.Actors.Add(pet);
        level.Scheduler.Register(pet);
        level.Tiles[1, 1].FloorType = FloorType.Fire; // the greedy diagonal-toward-owner candidate

        new PetAI().TakeTurn(pet, level, new Random(1));

        Check("Returning to the owner steps onto the safe horizontal tile instead of the hazardous diagonal one",
            pet.X == 1 && pet.Y == 0);
    }

    private static void APetStandingOnFireTakesEnvironmentalDamageLikeAMonster()
    {
        var (_, pet, level) = BuildPetTestScenario();
        level.Tiles[pet.X, pet.Y].FloorType = FloorType.Fire;
        int hpBefore = pet.Health.Current;

        EnvironmentalFloorEffects.ApplyToNonPlayerActors(level);

        Check("A pet standing on Fire takes environmental damage, just like a monster would", pet.Health.Current < hpBefore);
    }

    private static void HostileTargetSelectorPrefersWhicheverIsCloser()
    {
        var (player, pet, level) = BuildPetTestScenario();
        var monster = PetTestMonster("selector-monster", 5, 0, level); // far above the player, closer to nothing in particular

        // Player at (5,5), pet at (5,4) -- a monster standing right next to the pet should engage it.
        monster.X = 5;
        monster.Y = 3;
        var nearPet = HostileTargetSelector.NearestPlayerSideTarget(level, monster);
        Check("A monster nearer the pet than the player engages the pet", ReferenceEquals(nearPet, pet));

        monster.X = 5;
        monster.Y = 9;
        var nearPlayer = HostileTargetSelector.NearestPlayerSideTarget(level, monster);
        Check("A monster nearer the player than the pet engages the player", ReferenceEquals(nearPlayer, player));
    }

    private static void HostileTargetSelectorFallsBackToThePlayerWithNoPet()
    {
        var level = BuildOpenLevel(5, 5);
        var player = new Player("NoPetTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(305))) { X = 2, Y = 2 };
        level.Actors.Add(player);
        var monster = PetTestMonster("no-pet-monster", 3, 2, level);

        var target = HostileTargetSelector.NearestPlayerSideTarget(level, monster);

        Check("With no pet at all, monster target acquisition is completely unaffected", ReferenceEquals(target, player));
    }

    private static void PetAttacksAnAdjacentHostileMonster()
    {
        var (_, pet, level) = BuildPetTestScenario();
        var monster = PetTestMonster("adjacent-target", pet.X + 1, pet.Y, level, hp: 5);
        int hpBefore = monster.Health.Current;

        new PetAI().TakeTurn(pet, level, new Random(1));

        Check("The pet attacks a monster standing adjacent to it", monster.Health.Current <= hpBefore);
        Check("The pet's preferred target is now that monster", ReferenceEquals(pet.PreferredTarget, monster));
    }

    private static void PetMovesTowardAReachableTargetWhenNotYetAdjacent()
    {
        var (_, pet, level) = BuildPetTestScenario();
        var monster = PetTestMonster("distant-target", pet.X + 3, pet.Y, level);
        int startX = pet.X;

        new PetAI().TakeTurn(pet, level, new Random(1));

        Check("The pet moves toward a detected but not-yet-adjacent hostile target", pet.X > startX);
    }

    // --- Revised Pet Movement Proposal ---

    private static void RoamingAreaMembershipMatchesTheThreeTileChebyshevLimit()
    {
        var owner = Monster.Restore(BaseRestoreData("roam-owner", attack: 0, defense: 0));
        owner.X = 5;
        owner.Y = 5;
        var pet = new Pet { Owner = owner };

        for (int distance = 0; distance <= PetConfig.RoamingRadius; distance++)
        {
            pet.MoveTo(owner.X + distance, owner.Y);
            Check($"A pet at distance {distance} (horizontal) is inside the roaming area",
                Math.Max(Math.Abs(pet.X - owner.X), Math.Abs(pet.Y - owner.Y)) <= PetConfig.RoamingRadius);
        }

        pet.MoveTo(owner.X + PetConfig.RoamingRadius + 1, owner.Y);
        Check("A pet one tile beyond the radius is outside the roaming area",
            Math.Max(Math.Abs(pet.X - owner.X), Math.Abs(pet.Y - owner.Y)) > PetConfig.RoamingRadius);

        pet.MoveTo(owner.X + PetConfig.RoamingRadius, owner.Y + PetConfig.RoamingRadius);
        Check("Diagonal distance uses the same Chebyshev limit, not the sum of both axes",
            Math.Max(Math.Abs(pet.X - owner.X), Math.Abs(pet.Y - owner.Y)) <= PetConfig.RoamingRadius);
    }

    private static void AnIdlePetInsideTheAreaOnlyEverPicksDestinationsThatStayInsideIt()
    {
        var (player, pet, level) = BuildPetTestScenario(); // player (5,5), pet (5,4) -- well inside the area

        for (int seed = 0; seed < 200; seed++)
        {
            new PetAI().TakeTurn(pet, level, new Random(seed));
            int distance = Math.Max(Math.Abs(pet.X - player.X), Math.Abs(pet.Y - player.Y));
            if (distance > PetConfig.RoamingRadius)
            {
                Check("Random roaming never intentionally leaves the owner's area", false);
                return;
            }
        }

        Check("Random roaming never intentionally leaves the owner's area", true);
    }

    private static void AnIdlePetSometimesRemainsStillAndNeverEntersAnOccupiedOrBlockedTile()
    {
        var (player, pet, level) = BuildPetTestScenario();
        // A second, unrelated pet (never a valid combat threat -- IsValidThreat only ever matches
        // a Monster) sitting on one of the 8 neighboring tiles, purely to occupy it.
        var otherOwner = new Player("OtherOwnerTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(320)));
        var blocker = new Pet { Owner = otherOwner, Health = new HealthComponent(10) };
        blocker.MoveTo(pet.X + 1, pet.Y);
        level.Actors.Add(blocker);
        bool everRemainedStill = false;

        for (int seed = 0; seed < 200; seed++)
        {
            int beforeX = pet.X, beforeY = pet.Y;
            new PetAI().TakeTurn(pet, level, new Random(seed));

            if (pet.X == beforeX && pet.Y == beforeY)
            {
                everRemainedStill = true;
            }
            Check($"Roaming never lands on an occupied tile (seed {seed})", !(pet.X == blocker.X && pet.Y == blocker.Y) && !(pet.X == player.X && pet.Y == player.Y));
        }

        Check("Across many rolls, remaining still is a genuinely reachable outcome", everRemainedStill);
    }

    private static void OwnerMovementCanPushAPreviouslyInRangePetOutOfRangeAndItBeginsReturning()
    {
        var (player, pet, level) = BuildPetTestScenario(); // pet starts well inside the area
        player.X = 5 + PetConfig.RoamingRadius + 1; // now far enough that the pet is out of range
        player.Y = 5;
        int startX = pet.X;

        new PetAI().TakeTurn(pet, level, new Random(1));

        Check("An out-of-range pet begins returning (moving toward its owner) on its very next scheduled turn", pet.X > startX);
    }

    private static void ReturnBehaviorStopsAssoonAsThePetReentersTheAreaAndRoamingResumes()
    {
        var (player, pet, level) = BuildPetTestScenario();
        pet.MoveTo(player.X - PetConfig.RoamingRadius - 1, player.Y); // just outside the area
        Check("Setup: the pet starts outside the roaming area", Math.Max(Math.Abs(pet.X - player.X), Math.Abs(pet.Y - player.Y)) > PetConfig.RoamingRadius);

        new PetAI().TakeTurn(pet, level, new Random(1)); // one return step
        pet.MoveTo(player.X - PetConfig.RoamingRadius, player.Y); // now exactly at the boundary -- inside the area

        int beforeDistance = Math.Max(Math.Abs(pet.X - player.X), Math.Abs(pet.Y - player.Y));
        new PetAI().TakeTurn(pet, level, new Random(1)); // this turn must roam, not continue returning
        int afterDistance = Math.Max(Math.Abs(pet.X - player.X), Math.Abs(pet.Y - player.Y));

        Check("Once back inside the area, the pet resumes roaming (stays within the area) instead of continuing to close in on the owner",
            beforeDistance <= PetConfig.RoamingRadius && afterDistance <= PetConfig.RoamingRadius);
    }

    private static void APetMayLeaveTheRoamingAreaWhilePursuingAValidCombatTarget()
    {
        var (player, pet, level) = BuildPetTestScenario();
        var farTarget = PetTestMonster("far-combat-target", player.X + PetConfig.RoamingRadius + 1, player.Y, level); // stays within BuildOpenLevel's 10x10 bounds
        pet.PreferredTarget = farTarget; // already engaged, well beyond the roaming area

        new PetAI().TakeTurn(pet, level, new Random(1));

        Check("A pet chasing a valid target keeps pursuing it even though doing so leaves the roaming area",
            ReferenceEquals(pet.PreferredTarget, farTarget));
    }

    private static void APetWithAMonsterOwnerCentersItsRoamingAreaOnThatMonster()
    {
        var level = BuildOpenLevel(10, 10);
        var monsterOwner = Monster.Restore(BaseRestoreData("pet-owning-monster", attack: 0, defense: 0));
        monsterOwner.X = 5;
        monsterOwner.Y = 5;
        level.Actors.Add(monsterOwner);
        var pet = new Pet { Owner = monsterOwner, Health = new HealthComponent(10) };
        pet.MoveTo(5, 5 + PetConfig.RoamingRadius + 1); // out of range of the MONSTER owner
        level.Actors.Add(pet);
        int startY = pet.Y;

        new PetAI().TakeTurn(pet, level, new Random(1));

        Check("A monster-owned pet's roaming area is centered on its monster owner, not any player", pet.Y < startY);
    }

    private static void PetTargetsWhateverItsOwnerMostRecentlyAttacked()
    {
        var (player, pet, level) = BuildPetTestScenario();
        var farTarget = PetTestMonster("owner-attacked-target", 9, 9, level);

        CombatStatsTracker.ApplyDamage(farTarget, 1, player); // the owner's own combat notification

        new PetAI().TakeTurn(pet, level, new Random(1));
        Check("The pet adopts whatever its owner just attacked as its preferred target", ReferenceEquals(pet.PreferredTarget, farTarget));
    }

    private static void PetTargetsWhateverMostRecentlyAttackedItsOwner()
    {
        var (player, pet, level) = BuildPetTestScenario();
        var attacker = PetTestMonster("owner-attacker", 9, 0, level);

        CombatStatsTracker.ApplyDamage(player, 1, attacker);

        new PetAI().TakeTurn(pet, level, new Random(1));
        Check("The pet adopts whatever just attacked its owner as its preferred target", ReferenceEquals(pet.PreferredTarget, attacker));
    }

    private static void PetAbandonsADeadTargetAndReacquires()
    {
        var (_, pet, level) = BuildPetTestScenario();
        var deadTarget = PetTestMonster("already-dead-target", pet.X + 1, pet.Y, level, hp: 1);
        pet.PreferredTarget = deadTarget;
        deadTarget.Health.TakeDamage(999);
        var freshTarget = PetTestMonster("fresh-target", pet.X - 1, pet.Y, level); // adjacent, but not on the owner's own tile

        new PetAI().TakeTurn(pet, level, new Random(1));

        Check("The pet never keeps a dead actor as its preferred target", pet.PreferredTarget != deadTarget);
        Check("The pet picks up a fresh, living, adjacent threat instead", ReferenceEquals(pet.PreferredTarget, freshTarget));
    }

    private static void PetAbandonsAnUnreachableTargetForTheNearestReachableThreat()
    {
        var level = BuildOpenLevel(10, 10);
        // Wall off a small sealed room in the corner -- an unreachable target has to sit inside it.
        for (int x = 7; x <= 9; x++)
        {
            for (int y = 7; y <= 9; y++)
            {
                level.Tiles[x, y] = Tile.CreateWall();
            }
        }
        level.Tiles[8, 8] = Tile.CreateFloor(); // sealed on all sides

        var player = new Player("UnreachableTargetTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(306))) { X = 2, Y = 2 };
        level.Actors.Add(player);
        var pet = PetFactory.CreateDog(player);
        pet.MoveTo(2, 1);
        level.Actors.Add(pet);

        var unreachable = PetTestMonster("sealed-target", 8, 8, level);
        pet.PreferredTarget = unreachable;
        var reachable = PetTestMonster("reachable-target", 3, 1, level);

        new PetAI().TakeTurn(pet, level, new Random(1));

        Check("The pet abandons a target it cannot path to", pet.PreferredTarget != unreachable);
        Check("The pet picks the nearest reachable threat instead", ReferenceEquals(pet.PreferredTarget, reachable));
    }

    private static void FriendlyFireNeverHitsYourOwnPetWithATileAreaOfEffect()
    {
        var (player, pet, level) = BuildPetTestScenario();
        var targeting = new SpellTargetingConfiguration(SpellTargetType.Tile, range: 10, areaOfEffectRadius: 5);
        var context = new SpellCastingContext(player, level, level.TurnNumber, new Random(1)) { TargetTile = (player.X, player.Y) };

        TargetResolver.ResolveAffectedActors(targeting, context, out _);

        Check("An AoE centered on the caster never includes the caster's own pet", !context.AffectedActors.Contains(pet));
    }

    private static void FriendlyFireRejectsASingleTargetSpellAimedAtYourOwnPet()
    {
        var (player, pet, level) = BuildPetTestScenario();
        var targeting = new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 10);
        var context = new SpellCastingContext(player, level, level.TurnNumber, new Random(1)) { TargetActor = pet };

        bool resolved = TargetResolver.ResolveAffectedActors(targeting, context, out string failureReason);

        Check("A hostile single-target spell aimed at your own pet finds no valid target", !resolved && failureReason == "No target in range.");
    }

    private static void ProjectilesFromThePlayerNeverHitTheirOwnPet()
    {
        var (player, pet, _) = BuildPetTestScenario();
        Check("A player-fired shot cannot validly hit the player's own pet", !ProjectileIsValidTargetForTest(player, pet));
    }

    private static void ProjectilesFromAMonsterCanHitThePlayersPet()
    {
        var (_, pet, level) = BuildPetTestScenario();
        var monster = PetTestMonster("shooter", 0, 0, level);
        Check("A monster-fired shot can validly hit the player's own pet", ProjectileIsValidTargetForTest(monster, pet));
    }

    /// <summary>Exercises ProjectileEngine's own IsValidTarget rule via Launch's real collision behavior, using a 1-tile hop so the loop resolves in a single step. Tiles stay unexplored/invisible (BuildOpenLevel doesn't run FOV), so RenderFrame's own early-out means a null viewer is safe here.</summary>
    private static bool ProjectileIsValidTargetForTest(Actor source, Actor candidate)
    {
        var level = BuildOpenLevel(5, 5);
        source.MoveTo(0, 0);
        candidate.MoveTo(1, 0);
        level.Actors.Add(source);
        level.Actors.Add(candidate);

        var definition = new ProjectileDefinition("Test Bolt", range: 1, penetration: 0, '*', ConsoleColor.White, AttackType.Pierce);
        var projectile = new ProjectileInstance(definition, source, ProjectileSourceType.Weapon, 0, 0, (1, 0))
        {
            Damage = 5,
            DamageType = DamageType.Physical
        };
        var outcome = ProjectileEngine.Launch(level, null, Array.Empty<MessageEntry>(), projectile, new Random(1));

        return outcome.Projectile.AlreadyHit.Contains(candidate);
    }

    private static void AMonsterKilledByThePlayersPetGrantsRewardsToThePlayer()
    {
        var player = new Player("PetKillCreditTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(313)));
        var pet = PetFactory.CreateDog(player);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;

        var monster = Monster.CreateRandom(player.X, player.Y, 1, new Random(313));
        monster.Level = 10; // guarantees a non-zero gold-drop ceiling regardless of rng roll
        monster.LastDamageOwner = pet; // the kill's direct source is the DOG, not the player directly
        monster.Health.TakeDamage(monster.Health.Max);
        level.Actors.Add(monster);
        long xpBefore = player.Experience.Current;

        gameLoop.AwardDeathRewards(level);

        Check("A monster killed by the player's own pet still credits the OWNER with XP/gold (Allegiance.ResolveRewardBeneficiary)",
            player.Experience.Current > xpBefore && gameLoop.StatusMessages[^1].Contains("(+"));
    }

    private static void AMonsterKilledByAMonsterOwnedPetDoesNotRewardThePlayer()
    {
        var player = new Player("NoCreditFromOtherPetTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(314)));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;

        var otherOwner = Monster.CreateRandom(0, 0, 1, new Random(314));
        var otherPet = new Pet { Owner = otherOwner, DefinitionId = "test-pet" }; // a hypothetical monster-owned pet -- never the player's own

        var monster = Monster.CreateRandom(player.X, player.Y, 1, new Random(314));
        monster.LastDamageOwner = otherPet;
        monster.Health.TakeDamage(monster.Health.Max);
        level.Actors.Add(monster);
        long xpBefore = player.Experience.Current;

        gameLoop.AwardDeathRewards(level);

        Check("A monster-owned pet's kill never grants the player rewards merely because pets share the same system",
            player.Experience.Current == xpBefore);
    }

    private static void ADeadPetNeverAwardsXpGoldOrLoot()
    {
        var (player, pet, level) = BuildPetTestScenario();
        long xpBefore = player.Experience.Current;
        long goldBefore = player.Gold;
        pet.Health.TakeDamage(9999);
        pet.LastDamageOwner = player; // even if "credited" to the owner like a monster kill would be

        var gameLoop = new GameLoop(player);
        gameLoop.AwardDeathRewards(level); // the same method that DOES reward a dead Monster -- a dead Pet must pass through it untouched

        Check("A pet's own death is never processed by AwardDeathRewards (it only iterates Monsters)", player.Experience.Current == xpBefore && player.Gold == goldBefore);
    }

    private static void HandlePetDeathTransitionsToAwaitingRespawnWithTheCorrectDeadline()
    {
        var player = new Player("PetDeathTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(307)));
        PetFactory.CreateDog(player);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        var pet = player.Pet;
        player.TurnCount = 100;
        pet.Health.TakeDamage(9999);

        gameLoop.HandlePetDeath(level);

        Check("A dead pet transitions to AwaitingRespawn", pet.LifecycleState == PetLifecycleState.AwaitingRespawn);
        Check("Its respawn deadline is exactly 20 owner turns from now", pet.RespawnAtOwnerTurn == 120);
        Check("Its preferred target is cleared", pet.PreferredTarget == null);
    }

    private static void RespawningRestoresFullHealthCorrectLevelStatsAndActiveStatus()
    {
        var player = new Player("PetRespawnTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(308)));
        PetFactory.CreateDog(player);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        var pet = player.Pet;
        player.AddExperience(LevelProgression.GetXpRequiredForNextLevel(player.Class, player.Level)); // owner is now level 2
        pet.Health.TakeDamage(9999);
        gameLoop.HandlePetDeath(level);
        level.RemoveDeadActors();

        gameLoop.RespawnPet(pet);

        Check("A respawned pet is Active again", pet.LifecycleState == PetLifecycleState.Active);
        Check("A respawned pet is restored to full health", pet.Health.Current == pet.Health.Max);
        Check("A respawned pet's stats match the owner's CURRENT level, not level 1", pet.Health.Max == PetProgression.MaxHpFor(2));
        Check("A respawned pet is registered back onto the level", level.Actors.Contains(pet));
    }

    private static void ALivingPetFollowsItsOwnerThroughStairs()
    {
        var player = new Player("PetStairsTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(309)));
        PetFactory.CreateDog(player);
        var gameLoop = new GameLoop(player);
        var firstFloorLevel = gameLoop.CurrentLevel;
        player.X = firstFloorLevel.StairsDownPosition.X;
        player.Y = firstFloorLevel.StairsDownPosition.Y;
        var pet = player.Pet;

        gameLoop.HandleUseStairs(firstFloorLevel, descending: true);

        Check("The pet is removed from the old floor", !firstFloorLevel.Actors.Contains(pet));
        Check("The pet is registered on the new floor", gameLoop.CurrentLevel.Actors.Contains(pet));
        Check("The pet ends up near its owner on the new floor", Math.Max(Math.Abs(pet.X - player.X), Math.Abs(pet.Y - player.Y)) <= 1);
    }

    private static void APetAwaitingRespawnIsUnaffectedByStairTravel()
    {
        var player = new Player("PetAwaitingRespawnStairsTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(310)));
        PetFactory.CreateDog(player);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        var pet = player.Pet;
        pet.Health.TakeDamage(9999);
        gameLoop.HandlePetDeath(level);
        level.RemoveDeadActors();
        player.X = level.StairsDownPosition.X;
        player.Y = level.StairsDownPosition.Y;

        gameLoop.HandleUseStairs(level, descending: true);

        Check("Stair travel never revives a pet early", pet.LifecycleState == PetLifecycleState.AwaitingRespawn);
    }

    private static void PetRoundTripsThroughSaveAndLoadPreservingLifecycleState()
    {
        var player = new Player("PetPersistenceTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(311)));
        var pet = PetFactory.CreateDog(player);
        pet.Health.TakeDamage(3);
        pet.MoveTo(7, 2);
        pet.Energy = 42;

        var data = SaveManager.ToPetData(pet);
        var freshOwner = new Player("PetPersistenceTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(311))) { Level = pet.Level };
        var restored = SaveManager.FromPetData(data, freshOwner);

        Check("A restored pet keeps its exact current HP", restored.Health.Current == pet.Health.Current);
        Check("A restored pet keeps its saved position", restored.X == 7 && restored.Y == 2);
        Check("A restored pet keeps its saved energy", restored.Energy == 42);
        Check("A restored pet is re-attached to its (possibly new) owner instance", ReferenceEquals(freshOwner.Pet, restored));
    }

    private static void PetAwaitingRespawnSurvivesASaveLoadRoundTrip()
    {
        var player = new Player("PetRespawnPersistenceTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(312)));
        var pet = PetFactory.CreateDog(player);
        pet.LifecycleState = PetLifecycleState.AwaitingRespawn;
        pet.RespawnAtOwnerTurn = 250;

        var data = SaveManager.ToPetData(pet);
        var restored = SaveManager.FromPetData(data, player);

        Check("A restored pet's AwaitingRespawn state survives", restored.LifecycleState == PetLifecycleState.AwaitingRespawn);
        Check("A restored pet's respawn deadline survives", restored.RespawnAtOwnerTurn == 250);
    }

    private static void OldSaveWithNoPetDataLeavesThePlayerWithoutOne()
    {
        var data = new SaveData
        {
            PlayerName = "OldSaveNoPetTester",
            ClassName = CharacterClass.Warrior.Name,
            RaceName = Race.Human.Name,
            Level = 1
        };

        var player = SaveManager.ToPlayer(data);

        Check("A save written before this feature existed loads with no pet at all", player.Pet == null);
    }

    // --- Corpse System -----------------------------------------------------------------------

    private static (Player Player, GameLoop GameLoop, Level Level) BuildCorpseTestScenario(int seed)
    {
        var player = new Player("CorpseTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(seed)));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        return (player, gameLoop, level);
    }

    private static Monster DeadMonsterWithLoot(Level level, int x, int y, params Item[] loot)
    {
        var monster = Monster.Restore(BaseRestoreData("corpse-target", attack: 0, defense: 0));
        monster.X = x;
        monster.Y = y;
        foreach (var item in loot)
        {
            monster.Inventory.AddItem(item);
        }
        monster.Health.TakeDamage(9999);
        level.Actors.Add(monster);
        return monster;
    }

    private static void ADeadMonsterWithLootCreatesExactlyOneLootBearingCorpse()
    {
        var (player, gameLoop, level) = BuildCorpseTestScenario(400);
        // The randomly generated dungeon can occasionally drop a generated item right on the
        // player's own spawn tile -- guarantee a clean tile before checking an exact item count.
        level.GroundItems.RemoveAll(drop => drop.X == player.X && drop.Y == player.Y);
        var monster = DeadMonsterWithLoot(level, player.X, player.Y, Items.HealthPotion.Clone());
        monster.LastDamageOwner = player;

        gameLoop.AwardDeathRewards(level);

        var corpses = level.GetCorpsesAt(monster.X, monster.Y);
        Check("Exactly one corpse is created", corpses.Count == 1);
        Check("The corpse is loot-bearing", corpses[0].Container.Contents.Items.Count == 1);
        Check("No separate ground item was also created for that loot", level.GetItemsAt(monster.X, monster.Y).Count == 0);
    }

    private static void ADeadMonsterWithNoLootCreatesAnImmediatelyPortableCorpse()
    {
        var (player, gameLoop, level) = BuildCorpseTestScenario(401);
        var monster = Monster.Restore(BaseRestoreData("no-loot-target", attack: 0, defense: 0));
        monster.X = player.X;
        monster.Y = player.Y;
        monster.CanCarryItems = false; // guarantees LootGenerator.GenerateLoot returns nothing
        monster.Health.TakeDamage(9999);
        level.Actors.Add(monster);

        gameLoop.AwardDeathRewards(level);

        Check("No loot-bearing corpse is created", level.GetCorpsesAt(monster.X, monster.Y).Count == 0);
        var groundItems = level.GetItemsAt(monster.X, monster.Y);
        Check("An immediately portable corpse item appears on the ground instead", groundItems.Count(i => i.Type == ItemType.Corpse) == 1);
    }

    private static void TheCorpseRecordsTheStableDefinitionNotDisplayText()
    {
        var (player, gameLoop, level) = BuildCorpseTestScenario(402);
        var monster = Monster.CreateBoss(player.X, player.Y, 5, new Random(402));
        monster.Health.TakeDamage(9999);
        level.Actors.Add(monster);

        gameLoop.AwardDeathRewards(level);

        var corpse = level.GetCorpsesAt(monster.X, monster.Y).FirstOrDefault();
        var metadata = corpse?.Metadata ?? level.GetItemsAt(monster.X, monster.Y).First(i => i.Type == ItemType.Corpse).CorpseMetadata;
        Check("A boss corpse's DefinitionId is the underlying archetype Name, never the generated boss name", metadata.DefinitionId == monster.Name);
        Check("The corpse's presentation name is the boss's generated DisplayName", metadata.OriginalDisplayName == monster.DisplayName);
        Check("The corpse is flagged as a Boss origin", metadata.Origin == CorpseOrigin.Boss);
    }

    private static void BossCorpseNameHasNoArticleOrdinaryCorpseDoes()
    {
        var bossMetadata = new CorpseMetadata("giant cockroach", "Lormax Golden Wing", CreatureType.Other, 10, Size.Medium, CorpseOrigin.Boss);
        var ordinaryMetadata = new CorpseMetadata("giant rat", "giant rat", CreatureType.Animal, 1, Size.Small, CorpseOrigin.Ordinary);

        Check("A boss's corpse name uses its proper name with no article", CorpseItemFactory.FormatName(bossMetadata) == "corpse of Lormax Golden Wing");
        Check("An ordinary corpse name gets an article", CorpseItemFactory.FormatName(ordinaryMetadata) == "corpse of a giant rat");
    }

    private static void CorpseWeightIsDerivedFromOriginalSizeAndFrozen()
    {
        Check("Small -> 10 lbs", CorpseConfig.WeightFor(Size.Small) == 10);
        Check("Medium -> 40 lbs", CorpseConfig.WeightFor(Size.Medium) == 40);
        Check("Large -> 100 lbs", CorpseConfig.WeightFor(Size.Large) == 100);

        var metadata = new CorpseMetadata("giant rat", "giant rat", CreatureType.Animal, 1, Size.Large, CorpseOrigin.Ordinary);
        var item = CorpseItemFactory.CreatePortableItem(metadata);
        Check("The portable item's weight matches the resolved size-based weight", item.Weight == 100);
    }

    private static void CorpseItemsAreNeverStackable()
    {
        var metadata = new CorpseMetadata("giant rat", "giant rat", CreatureType.Animal, 1, Size.Small, CorpseOrigin.Ordinary);
        var a = CorpseItemFactory.CreatePortableItem(metadata);
        var b = CorpseItemFactory.CreatePortableItem(new CorpseMetadata("giant rat", "giant rat", CreatureType.Animal, 1, Size.Small, CorpseOrigin.Ordinary));

        Check("A corpse item is never considered stackable", !ItemStacking.IsStackable(a));
        Check("Two corpses of even the exact same original creature never stack together", !ItemStacking.CanStackTogether(a, b));
    }

    private static void CorpsesNeverBlockMovement()
    {
        var (player, _, level) = BuildCorpseTestScenario(403);
        var metadata = new CorpseMetadata("giant rat", "giant rat", CreatureType.Animal, 1, Size.Small, CorpseOrigin.Ordinary);
        level.Tiles[player.X + 1, player.Y] = Tile.CreateFloor();
        // The randomly generated dungeon could otherwise have already placed a monster on this
        // exact tile, which WOULD legitimately block movement -- for its own reason, unrelated to
        // the corpse this test is actually about -- and make this check flaky.
        level.Actors.RemoveAll(a => a.X == player.X + 1 && a.Y == player.Y && a is not Player);
        level.Corpses.Add(new Dungeon.Corpse(player.X + 1, player.Y, metadata, new[] { Items.HealthPotion.Clone() }));

        Check("A tile occupied only by a corpse is not blocked for actor movement", !level.IsBlockedForActorMovement(player.X + 1, player.Y));
    }

    private static void MultipleCorpsesCanShareOneTile()
    {
        var (player, _, level) = BuildCorpseTestScenario(404);
        var metadataA = new CorpseMetadata("giant rat", "giant rat", CreatureType.Animal, 1, Size.Small, CorpseOrigin.Ordinary);
        var metadataB = new CorpseMetadata("cave bear", "cave bear", CreatureType.Animal, 3, Size.Medium, CorpseOrigin.Ordinary);
        level.Corpses.Add(new Dungeon.Corpse(player.X, player.Y, metadataA, new[] { Items.HealthPotion.Clone() }));
        level.Corpses.Add(new Dungeon.Corpse(player.X, player.Y, metadataB, new[] { Items.HealthPotion.Clone() }));

        var corpsesHere = level.GetCorpsesAt(player.X, player.Y);
        Check("Both corpses are found sharing the same tile", corpsesHere.Count == 2);
    }

    private static void OpeningTheOnlyCorpseOnATileOpensItDirectly()
    {
        var (player, gameLoop, level) = BuildCorpseTestScenario(405);
        // Guarantee no incidental chest shares this tile -- would otherwise route through the
        // "choose a container" menu instead of opening the corpse directly.
        level.Chests.RemoveAll(c => c.X == player.X && c.Y == player.Y);
        var metadata = new CorpseMetadata("giant rat", "giant rat", CreatureType.Animal, 1, Size.Small, CorpseOrigin.Ordinary);
        level.Corpses.Add(new Dungeon.Corpse(player.X, player.Y, metadata, new[] { Items.HealthPotion.Clone() }));

        bool turnConsumed = gameLoop.HandleOpenChest(level, showScreen: false);

        Check("A single corpse on the player's tile opens directly, no menu needed", turnConsumed);
        Check("The confirmation message names the corpse", gameLoop.StatusMessages[^1].Contains("giant rat"));
    }

    private static void OpeningWithNoChestOrCorpseFailsCleanly()
    {
        var (player, gameLoop, level) = BuildCorpseTestScenario(406);
        // Guarantee no incidental chest shares this tile -- would otherwise make "nothing here"
        // untrue for a reason unrelated to what this test is actually checking.
        level.Chests.RemoveAll(c => c.X == player.X && c.Y == player.Y);

        bool turnConsumed = gameLoop.HandleOpenChest(level, showScreen: false);

        Check("Opening with nothing on the tile fails without consuming a turn", !turnConsumed);
    }

    private static void EmptyingTheLastItemConvertsTheCorpseToAPortableItem()
    {
        var (player, gameLoop, level) = BuildCorpseTestScenario(407);
        var metadata = new CorpseMetadata("giant rat", "giant rat", CreatureType.Animal, 1, Size.Small, CorpseOrigin.Ordinary);
        var corpse = new Dungeon.Corpse(player.X, player.Y, metadata, new[] { Items.HealthPotion.Clone() });
        level.Corpses.Add(corpse);

        // Simulates what happens inside the corpse's ContainerScreen session -- removing its only item.
        corpse.Container.Contents.RemoveItem(corpse.Container.Contents.Items[0]);
        gameLoop.ConvertCorpseIfEmptied(corpse, level);

        Check("The emptied corpse is removed from the level's loot-bearing corpse list", !level.Corpses.Contains(corpse));
        Check("A portable corpse item now sits on the same tile", level.GetItemsAt(player.X, player.Y).Any(i => i.Type == ItemType.Corpse));
    }

    private static void APortableCorpseCanBePickedUpLikeAnOrdinaryItem()
    {
        var (player, gameLoop, level) = BuildCorpseTestScenario(408);
        // Guarantee a clean tile -- see ADeadMonsterWithLootCreatesExactlyOneLootBearingCorpse's
        // own comment on why (this test relies on exactly one pickup candidate being present).
        level.GroundItems.RemoveAll(drop => drop.X == player.X && drop.Y == player.Y);
        var metadata = new CorpseMetadata("giant rat", "giant rat", CreatureType.Animal, 1, Size.Small, CorpseOrigin.Ordinary);
        var item = CorpseItemFactory.CreatePortableItem(metadata);
        level.AddVisibleItem(player.X, player.Y, item, ItemLandingOrigin.Generated);

        bool turnConsumed = gameLoop.HandlePickUp(level);

        Check("A portable corpse is picked up through the ordinary pickup command", turnConsumed);
        Check("It ends up in the player's inventory", player.Inventory.Items.Contains(item));
    }

    private static void ADeadPetLeavesACorpseWithPetOriginAndOwnerName()
    {
        var player = new Player("PetCorpseOwnerTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(409)));
        PetFactory.CreateDog(player);
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        var pet = player.Pet;
        pet.Health.TakeDamage(9999);

        gameLoop.HandlePetDeath(level);

        var corpse = level.GetCorpsesAt(pet.X, pet.Y).FirstOrDefault();
        var metadata = corpse?.Metadata ?? level.GetItemsAt(pet.X, pet.Y).First(i => i.Type == ItemType.Corpse).CorpseMetadata;
        Check("A dead pet leaves a corpse flagged with Pet origin", metadata.Origin == CorpseOrigin.Pet);
        Check("The pet's corpse records its owner's name", metadata.OwnerName == player.Name);
        Check("Pet death still enters the normal 20-turn respawn state", pet.LifecycleState == PetLifecycleState.AwaitingRespawn);
    }

    private static void ADeadMonstersKillCreditAndCorpseAreIndependent()
    {
        var (player, gameLoop, level) = BuildCorpseTestScenario(410);
        var monster = Monster.CreateRandom(player.X, player.Y, 5, new Random(410));
        monster.LastDamageOwner = player;
        monster.Health.TakeDamage(monster.Health.Max);
        level.Actors.Add(monster);
        long xpBefore = player.Experience.Current;

        gameLoop.AwardDeathRewards(level);

        Check("A credited kill still grants XP exactly once even though loot now routes through a corpse", player.Experience.Current > xpBefore);
    }

    private static void CorpseMetadataSurvivesASaveLoadRoundTripForALootBearingCorpse()
    {
        var metadata = new CorpseMetadata("cave bear", "cave bear", CreatureType.Animal, 4, Size.Medium, CorpseOrigin.Ordinary);
        var corpse = new Dungeon.Corpse(7, 2, metadata, new[] { Items.HealthPotion.Clone(), Items.Dagger.Clone() });

        var data = SaveManager.ToCorpseData(corpse);
        var restored = SaveManager.FromCorpseData(data);

        Check("Position survives", restored.X == 7 && restored.Y == 2);
        Check("The corpse identifier survives", restored.Metadata.CorpseId == metadata.CorpseId);
        Check("The definition id survives", restored.Metadata.DefinitionId == "cave bear");
        Check("The resolved weight survives without being recalculated", restored.Metadata.Weight == metadata.Weight);
        Check("Both contained items survive", restored.Container.Contents.Items.Count == 2);
    }

    private static void PortableCorpseMetadataSurvivesASaveLoadRoundTrip()
    {
        var metadata = new CorpseMetadata("giant rat", "giant rat", CreatureType.Animal, 2, Size.Small, CorpseOrigin.Ordinary);
        var item = CorpseItemFactory.CreatePortableItem(metadata);

        var data = SaveManager.ToItemData(item);
        var restored = SaveManager.ResolveItem(data);

        Check("A restored portable corpse keeps its ItemType.Corpse", restored.Type == ItemType.Corpse);
        Check("It keeps its original display name", restored.Name == item.Name);
        Check("It keeps its resolved weight", restored.Weight == item.Weight);
        Check("It keeps its corpse metadata's definition id", restored.CorpseMetadata.DefinitionId == "giant rat");
    }

    private static void OldSaveWithNoCorpseDataLoadsWithAnEmptyCorpseList()
    {
        var data = new LevelData { FloorIndex = 1, Width = 5, Height = 5, TileTypes = new TileType[25], TileExplored = new bool[25], Corpses = null };
        for (int i = 0; i < 25; i++)
        {
            data.TileTypes[i] = TileType.Floor;
        }

        var level = SaveManager.FromLevelData(data);

        Check("A save written before this feature existed loads with no corpses at all", level.Corpses.Count == 0);
    }

    private static void CorpseSelectionLabelsDistinguishIdenticalNamesOnlyWithinTheMenu()
    {
        var metadata = new CorpseMetadata("giant rat", "giant rat", CreatureType.Animal, 1, Size.Small, CorpseOrigin.Ordinary);
        var corpseA = new Dungeon.Corpse(0, 0, metadata, new[] { Items.HealthPotion.Clone() });
        var corpseB = new Dungeon.Corpse(0, 0, new CorpseMetadata("giant rat", "giant rat", CreatureType.Animal, 1, Size.Small, CorpseOrigin.Ordinary), new[] { Items.Dagger.Clone(), Items.HealthPotion.Clone() });

        var labels = GameLoop.CorpseSelectionLabels(new List<Dungeon.Corpse> { corpseA, corpseB });

        Check("Two identically-named corpses get distinguishing numbers in the selection menu", labels[0].Contains("(1)") && labels[1].Contains("(2)"));
        Check("Item counts are shown per corpse", labels[0].Contains("1 item") && labels[1].Contains("2 items"));
        Check("The corpse item's own permanent Name never includes a menu-local number", !corpseA.Container.Contents.Items[0].Name.Contains("("));
    }

    // --- Safe Monster Spawning and Hazard-Aware Movement --------------------------------------

    private static Monster CreateAttunedMonster(FloorType preferredFloorType, DamageType? elementalAffinity, int x = 0, int y = 0)
    {
        var data = BaseRestoreData("attuned-tester", attack: 0, defense: 0);
        data.PreferredFloorType = preferredFloorType;
        data.ElementalAffinity = elementalAffinity;
        var monster = Monster.Restore(data);
        monster.X = x;
        monster.Y = y;
        return monster;
    }

    private static void OrdinaryMonstersAreUnsafeOnFireOrLava()
    {
        var level = BuildOpenLevel(5, 5);
        var monster = Monster.Restore(BaseRestoreData("ordinary-tester", attack: 0, defense: 0));
        level.Tiles[2, 2].FloorType = FloorType.Fire;
        level.Tiles[3, 2].FloorType = FloorType.Lava;

        Check("An ordinary monster is unsafe on Fire", !ActorTerrainSafety.IsSafeForActor(level, monster, 2, 2));
        Check("An ordinary monster is unsafe on Lava", !ActorTerrainSafety.IsSafeForActor(level, monster, 3, 2));
    }

    private static void OrdinaryMonstersAreSafeOnHarmlessSpecialFloors()
    {
        var level = BuildOpenLevel(5, 5);
        var monster = Monster.Restore(BaseRestoreData("ordinary-tester", attack: 0, defense: 0));
        foreach (var floorType in new[] { FloorType.Water, FloorType.Ice, FloorType.Grass, FloorType.Swamp, FloorType.Mud, FloorType.Sand, FloorType.Ash })
        {
            level.Tiles[2, 2].FloorType = floorType;
            Check($"An ordinary monster is safe on {floorType}", ActorTerrainSafety.IsSafeForActor(level, monster, 2, 2));
        }
    }

    private static void FireAttunedMonsterIsSafeOnFireButNotOnLava()
    {
        var level = BuildOpenLevel(5, 5);
        var fireBeetle = CreateAttunedMonster(FloorType.Fire, DamageType.Fire);
        level.Tiles[2, 2].FloorType = FloorType.Fire;
        level.Tiles[3, 2].FloorType = FloorType.Lava;

        Check("A Fire-attuned monster is safe standing on Fire", ActorTerrainSafety.IsSafeForActor(level, fireBeetle, 2, 2));
        Check("A Fire-attuned monster is NOT safe standing on Lava, even though Lava's damage type is also Fire (different FloorType, no attunement match)",
            !ActorTerrainSafety.IsSafeForActor(level, fireBeetle, 3, 2));
    }

    private static void LavaAttunedMonsterIsSafeOnLava()
    {
        var level = BuildOpenLevel(5, 5);
        var lavaElemental = CreateAttunedMonster(FloorType.Lava, DamageType.Fire);
        level.Tiles[2, 2].FloorType = FloorType.Lava;

        Check("A Lava-attuned monster is safe standing on Lava", ActorTerrainSafety.IsSafeForActor(level, lavaElemental, 2, 2));
    }

    private static void MatchingTerrainZeroesTheEnvironmentalTickWithoutGrantingGeneralFireImmunity()
    {
        var level = BuildOpenLevel(5, 5);
        var fireBeetle = CreateAttunedMonster(FloorType.Fire, DamageType.Fire);
        level.Tiles[2, 2].FloorType = FloorType.Fire;

        int tickDamage = EnvironmentalFloorEffects.CalculateEnvironmentalDamage(level, fireBeetle, 2, 2, out _);
        Check("The environmental Fire tick is exactly zero for a Fire-attuned monster on Fire", tickDamage == 0);

        // A direct attack of DamageType.Fire (e.g. a spell) still resolves through the ordinary
        // resistance pipeline, completely untouched by the environmental-tick immunity above --
        // immunity to the TICK, never to Fire damage in general.
        int attackDamage = ResistanceCalculator.ApplyResistance(level, fireBeetle, DamageType.Fire, 20);
        Check("A direct Fire attack against the same monster on the same tile is unaffected by tick immunity", attackDamage > 0);
    }

    private static void AFloorAttunedMonsterOffItsPreferredTerrainIsUnsafe()
    {
        var level = BuildOpenLevel(5, 5);
        var fireBeetle = CreateAttunedMonster(FloorType.Fire, DamageType.Fire);
        level.Tiles[2, 2].FloorType = FloorType.Normal;

        Check("A floor-attuned monster standing off its preferred terrain is unsafe (attrition)", !ActorTerrainSafety.IsSafeForActor(level, fireBeetle, 2, 2));
    }

    private static void CanActorOccupyCombinesPhysicalBlockingWithSafety()
    {
        var level = BuildOpenLevel(5, 5);
        var monster = Monster.Restore(BaseRestoreData("occupy-tester", attack: 0, defense: 0));
        var blocker = Monster.Restore(BaseRestoreData("blocker", attack: 0, defense: 0));
        blocker.X = 2; blocker.Y = 2;
        level.Actors.Add(blocker);

        Check("A physically blocked (occupied) tile is never occupiable regardless of terrain safety", !ActorTerrainSafety.CanActorOccupy(level, monster, 2, 2));

        level.Tiles[3, 3].FloorType = FloorType.Fire;
        Check("A physically open but hazardous tile is not occupiable either", !ActorTerrainSafety.CanActorOccupy(level, monster, 3, 3));

        Check("An open, safe tile is occupiable", ActorTerrainSafety.CanActorOccupy(level, monster, 4, 4));
    }

    private static void FindSafeAdjacentTilePrefersSafetyAndReturnsNullWhenNoneQualifies()
    {
        var level = BuildOpenLevel(5, 5);
        var monster = Monster.Restore(BaseRestoreData("adjacent-tester", attack: 0, defense: 0));
        // Every neighbor of (2,2) is Fire except (2,1).
        foreach (var (x, y) in new[] { (1, 1), (2, 1), (3, 1), (1, 2), (3, 2), (1, 3), (2, 3), (3, 3) })
        {
            level.Tiles[x, y].FloorType = FloorType.Fire;
        }
        level.Tiles[2, 1].FloorType = FloorType.Normal;

        var safeTile = ActorTerrainSafety.FindSafeAdjacentTile(level, monster, 2, 2);
        Check("FindSafeAdjacentTile finds the one safe neighbor among several hazardous ones", safeTile == (2, 1));

        foreach (var (x, y) in new[] { (1, 1), (2, 1), (3, 1), (1, 2), (3, 2), (1, 3), (2, 3), (3, 3) })
        {
            level.Tiles[x, y].FloorType = FloorType.Fire; // now every neighbor is hazardous
        }
        Check("FindSafeAdjacentTile returns null when every neighbor is hazardous", ActorTerrainSafety.FindSafeAdjacentTile(level, monster, 2, 2) == null);
    }

    private static void FindNextStepTreatsHazardousTerrainAsFullyImpassableForAnActor()
    {
        // A 1-wide corridor from (0,2) to (4,2) where the only route crosses Fire at (2,2).
        var level = BuildOpenLevel(5, 5);
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                level.Tiles[x, y] = Tile.CreateWall();
            }
        }
        for (int x = 0; x < 5; x++)
        {
            level.Tiles[x, 2] = Tile.CreateFloor();
        }
        level.Tiles[2, 2].FloorType = FloorType.Fire;
        var monster = Monster.Restore(BaseRestoreData("pathfind-tester", attack: 0, defense: 0));

        var stepIgnoringHazard = TerrainPathfinder.FindNextStep(level, (0, 2), (4, 2));
        Check("With no actor given, the pathfinder ignores hazard (today's original, backward-compatible behavior)", stepIgnoringHazard != null);

        var stepRespectingHazard = TerrainPathfinder.FindNextStep(level, (0, 2), (4, 2), mover: monster);
        Check("With an actor given, a route that can only cross hazardous terrain reports no path at all", stepRespectingHazard == null);
    }

    private static void FindNextStepRoutesAroundHazardWhenASafeDetourExists()
    {
        // Fully open 5x5 room; the hazardous tile sits directly between mover and target, exactly
        // where a naive greedy step would go -- a real test that the safe alternative is actually
        // preferred, not just that SOME step exists.
        var level = BuildOpenLevel(5, 5);
        level.Tiles[2, 2].FloorType = FloorType.Fire;
        var monster = Monster.Restore(BaseRestoreData("detour-tester", attack: 0, defense: 0));

        var step = TerrainPathfinder.FindNextStep(level, (1, 2), (3, 2), mover: monster);
        Check("A safe detour around a single hazardous tile directly on the shortest path is found instead of stepping onto it",
            step != null && step.Value != (2, 2));
    }

    private static void AChasingMonsterRemainsInPlaceWhenEveryRouteIsHazardous()
    {
        var level = BuildOpenLevel(5, 3);
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                level.Tiles[x, y] = Tile.CreateWall();
            }
        }
        for (int x = 0; x < 5; x++)
        {
            level.Tiles[x, 1] = Tile.CreateFloor(); // the only row -- a single 1-wide corridor
        }
        level.Tiles[2, 1].FloorType = FloorType.Fire; // blocks the only route across
        var player = new Player("HazardChaseTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(500))) { X = 4, Y = 1 };
        level.Actors.Add(player);
        var monster = Monster.Restore(BaseRestoreData("hazard-chaser", attack: 0, defense: 0));
        // Starts already adjacent to the hazard (immediately west of it) so this single TakeTurn
        // call directly confronts the "only route is hazardous" decision, rather than spending
        // its one move just closing the remaining safe distance first.
        monster.X = 1;
        monster.Y = 1;
        monster.AI = new ChaseAI();
        level.Actors.Add(monster);

        new ChaseAI().TakeTurn(monster, level, new Random(1));

        Check("A chasing monster never crosses the only available (hazardous) route to its target", monster.X == 1 && monster.Y == 1);
    }

    private static void APetNeverSpawnsRoamsOrPathsOntoDamagingTerrain()
    {
        var (player, pet, level) = BuildPetTestScenario();
        // Surround the pet with Fire except its own tile and the tile directly toward the owner.
        foreach (var (dx, dy) in new[] { (-1, -1), (0, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (1, 1) })
        {
            level.Tiles[pet.X + dx, pet.Y + dy].FloorType = FloorType.Fire;
        }
        // (pet.X, pet.Y+1), toward the owner at (5,5), is left Normal.

        for (int seed = 0; seed < 100; seed++)
        {
            pet.MoveTo(5, 4);
            new PetAI().TakeTurn(pet, level, new Random(seed));
            if (level.Tiles[pet.X, pet.Y].FloorType == FloorType.Fire)
            {
                Check("A pet never voluntarily ends its turn on Fire", false);
                return;
            }
        }
        Check("A pet never voluntarily ends its turn on Fire", true);
    }

    private static void SleepAmbushNeverPlacesAMonsterOnFireOrLava()
    {
        var level = BuildOpenLevel(15, 15);
        for (int x = 0; x < 15; x++)
        {
            for (int y = 0; y < 15; y++)
            {
                // Checkerboard Fire/Normal so roughly half of all candidate tiles are hazardous --
                // a real stress test of the filter rather than an all-or-nothing level.
                level.Tiles[x, y].FloorType = (x + y) % 2 == 0 ? FloorType.Fire : FloorType.Normal;
            }
        }
        var player = new Player("AmbushHazardTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(501))) { X = 7, Y = 7 };
        level.Tiles[player.X, player.Y].FloorType = FloorType.Normal;
        level.Actors.Add(player);

        var positions = SleepAmbushSystem.FindAmbushPositions(level, player, count: 40, new Random(502));

        Check("Ambush candidate positions never include Fire/Lava, even under heavy hazard density",
            positions.Count > 0 && positions.All(p => level.Tiles[p.X, p.Y].FloorType != FloorType.Fire));
    }

    private static void GeneratedFloorsNeverPlaceAnOrdinaryMonsterOnFireOrLava()
    {
        for (int seed = 600; seed < 610; seed++)
        {
            var level = DungeonGenerator.Generate(floorIndex: 5, width: 60, height: 22, new Random(seed), difficultyLevel: 5, forceTraderSpawn: false);
            foreach (var monster in level.Actors.OfType<Monster>().Where(m => !m.IsFloorAttuned))
            {
                var floorType = level.Tiles[monster.X, monster.Y].FloorType;
                Check($"Seed {seed}: an ordinary (non-attuned) monster never spawns on Fire/Lava", floorType != FloorType.Fire && floorType != FloorType.Lava);
            }
        }
    }

    private static void PlayerMovementOntoFireOrLavaRemainsAllowed()
    {
        var player = new Player("PlayerHazardTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(503)));
        var gameLoop = new GameLoop(player);
        var level = gameLoop.CurrentLevel;
        int targetX = player.X + 1, targetY = player.Y;
        level.Tiles[targetX, targetY] = Tile.CreateFloor();
        level.Tiles[targetX, targetY].FloorType = FloorType.Fire;
        level.Actors.RemoveAll(a => a.X == targetX && a.Y == targetY && a is not Player);

        bool turnConsumed = gameLoop.HandleMove(level, PlayerCommand.MoveEast);

        Check("The player can still deliberately walk onto Fire -- ActorTerrainSafety only ever gates AI-controlled actors",
            turnConsumed && player.X == targetX && player.Y == targetY);
    }

    // --- Container "Get All" ------------------------------------------------------------------

    private static void GetAllMovesEveryContainerItemIntoInventory()
    {
        var player = new Player("GetAllTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(700)));
        var chest = new Chest(0, 0, isLocked: false, difficulty: 0, new List<Item> { Items.HealthPotion.Clone(), Items.Dagger.Clone() });

        string message = ContainerScreen.GetAllFlow(player, chest.Container, "Chest");

        Check("Get All reports both taken items by name", message.Contains(Items.HealthPotion.Name) && message.Contains(Items.Dagger.Name));
        Check("The chest ends up empty", chest.Container.Contents.Items.Count == 0);
        Check("Both items land in the player's inventory", player.Inventory.Items.Count == 2);
    }

    private static void GetAllOnAnEmptyContainerReportsNothingToTake()
    {
        var player = new Player("GetAllEmptyTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(701)));
        var chest = new Chest(0, 0, isLocked: false, difficulty: 0, new List<Item>());

        string message = ContainerScreen.GetAllFlow(player, chest.Container, "Chest");

        Check("Get All on an empty container reports nothing to take, rather than an empty/blank message", message == "There's nothing in here to take.");
    }

    private static void GetAllStopsAtCarryingCapacityAndLeavesTheRestInTheContainer()
    {
        var player = new Player("GetAllCapacityTester", CharacterClass.Warrior, Race.Human, CharacterStats.Roll(Race.Human, CharacterClass.Warrior, new Random(702)));
        var lightItem = Items.HealthPotion.Clone();
        // An impossibly heavy item guarantees a capacity failure regardless of this player's
        // actual rolled Strength, so this test never depends on exact carry-capacity arithmetic.
        var impossiblyHeavyItem = new Item("Boulder", '*', "Impossibly heavy.", ItemType.Consumable, weight: 999999, canDropAsLoot: false);
        var chest = new Chest(0, 0, isLocked: false, difficulty: 0, new List<Item> { lightItem, impossiblyHeavyItem });

        string message = ContainerScreen.GetAllFlow(player, chest.Container, "Chest");

        Check("The light item is taken", player.Inventory.Items.Contains(lightItem));
        Check("The impossibly heavy item is left behind in the container instead of lost", chest.Container.Contents.Items.Contains(impossiblyHeavyItem));
        Check("The summary message names the light item taken and explains the capacity failure",
            message.Contains(Items.HealthPotion.Name) && message.Contains("too heavy"));
    }

    private static void GetAllFromACorpseEmptiesItAndGameLoopConvertsItToAPortableItem()
    {
        var (player, gameLoop, level) = BuildCorpseTestScenario(703);
        var metadata = new CorpseMetadata("giant rat", "giant rat", CreatureType.Animal, 1, Size.Small, CorpseOrigin.Ordinary);
        var corpse = new Dungeon.Corpse(player.X, player.Y, metadata, new[] { Items.HealthPotion.Clone(), Items.Dagger.Clone() });
        level.Corpses.Add(corpse);

        ContainerScreen.GetAllFlow(player, corpse.Container, CorpseItemFactory.FormatName(metadata));
        gameLoop.ConvertCorpseIfEmptied(corpse, level);

        Check("Get All takes every item out of the corpse", player.Inventory.Items.Count == 2);
        Check("The now-empty corpse converts into a portable item on the same tile, exactly as removing its last item one at a time would",
            !level.Corpses.Contains(corpse) && level.GetItemsAt(player.X, player.Y).Any(i => i.Type == ItemType.Corpse));
    }

    // --- Case-insensitive menu key handling -------------------------------------------------

    /// <summary>
    /// Regression test for a real bug: TraderScreen and ContainerScreen used to compare a raw,
    /// un-normalized key.KeyChar against uppercase-only literals for both letter-indexed item
    /// selection and single-letter commands, while MenuPrompt.OptionKey always DISPLAYS a
    /// lowercase letter -- on any terminal/OS combination that reports an unshifted keypress as
    /// uppercase (or a shifted one as lowercase -- e.g. Caps Lock on), the displayed letter and
    /// the one that actually registered could silently disagree, matching exactly what was
    /// reported: an item listed under one case only selectable by pressing the other. Both
    /// screens now lowercase key.KeyChar once up front (mirroring MenuPrompt.Choose's own
    /// pattern) and delegate to this exact shared mapping instead of each keeping their own
    /// (in-practice-diverging) copy -- this test locks in that shared contract directly.
    /// </summary>
    private static void MenuPromptIndexFromKeyMapsDigitsAndLettersForTheFullOptionRange()
    {
        for (int i = 0; i < 35; i++)
        {
            string label = MenuPrompt.OptionKey(i);
            char keyChar = label[0]; // OptionKey always returns a single character ("1".."9", "a".."z")
            Check($"IndexFromKey inverts OptionKey exactly for index {i} ('{keyChar}')", MenuPrompt.IndexFromKey(keyChar) == i);
        }

        Check("A character outside 1-9/a-z maps to no option", MenuPrompt.IndexFromKey('!') == -1);
        Check("An uppercase letter alone (before the caller's own ToLowerInvariant) does NOT match -- callers must normalize first, exactly like MenuPrompt.Choose/TraderScreen/ContainerScreen all now do",
            MenuPrompt.IndexFromKey('F') == -1 && MenuPrompt.IndexFromKey('f') == MenuPrompt.IndexFromKey(char.ToLowerInvariant('F')));
    }
}
