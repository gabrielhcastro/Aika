-- MySQL dump 10.13  Distrib 8.0.41, for Win64 (x86_64)
--
-- Host: localhost    Database: aikaria
-- ------------------------------------------------------
-- Server version	8.0.41

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `__efmigrationshistory`
--

DROP TABLE IF EXISTS `__efmigrationshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `__efmigrationshistory`
--

LOCK TABLES `__efmigrationshistory` WRITE;
/*!40000 ALTER TABLE `__efmigrationshistory` DISABLE KEYS */;
INSERT INTO `__efmigrationshistory` VALUES ('20250211211637_InitialCreate','8.0.13');
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `accounts`
--

DROP TABLE IF EXISTS `accounts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `accounts` (
  `id` int NOT NULL AUTO_INCREMENT,
  `username` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `passwordHash` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `token` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `tokenCreationTime` datetime(6) NOT NULL,
  `accountStatus` int NOT NULL,
  `banDays` int NOT NULL,
  `nation` int NOT NULL,
  `accountType` int NOT NULL,
  `storageGold` int unsigned DEFAULT NULL,
  `cash` int DEFAULT NULL,
  `premiumExpiration` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `accounts`
--

LOCK TABLES `accounts` WRITE;
/*!40000 ALTER TABLE `accounts` DISABLE KEYS */;
INSERT INTO `accounts` VALUES (1,'admin','21232f297a57a5a743894a0e4a801fc3','e19a037bf7392145a039477392922f9b','2025-02-25 02:03:49.000000',0,0,0,0,0,0,NULL),(2,'admin2','21232f297a57a5a743894a0e4a801fc3','9c9aead2bbbe8c8dbb4e23ea804bb1ab','2025-02-16 21:27:40.000000',0,0,0,0,0,0,NULL);
/*!40000 ALTER TABLE `accounts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `characters`
--

DROP TABLE IF EXISTS `characters`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `characters` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `ownerAccountId` int unsigned NOT NULL,
  `name` varchar(16) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
  `slot` tinyint unsigned NOT NULL,
  `numericToken` varchar(4) CHARACTER SET latin1 COLLATE latin1_swedish_ci DEFAULT NULL,
  `numericErrors` tinyint unsigned NOT NULL,
  `deleted` tinyint unsigned DEFAULT '0',
  `speedMove` tinyint unsigned NOT NULL,
  `rotation` int unsigned DEFAULT NULL,
  `lastLogin` varchar(50) DEFAULT '1',
  `playerKill` tinyint unsigned NOT NULL DEFAULT '0',
  `classInfo` tinyint unsigned NOT NULL,
  `firstLogin` tinyint unsigned NOT NULL,
  `strength` int unsigned NOT NULL,
  `agility` int unsigned NOT NULL,
  `intelligence` int unsigned NOT NULL,
  `constitution` int unsigned NOT NULL,
  `luck` int unsigned NOT NULL,
  `status` int unsigned NOT NULL,
  `height` tinyint unsigned NOT NULL,
  `trunk` tinyint unsigned NOT NULL,
  `leg` tinyint unsigned NOT NULL,
  `body` tinyint unsigned NOT NULL,
  `currentHealth` int unsigned DEFAULT NULL,
  `currentMana` int unsigned DEFAULT NULL,
  `honor` int unsigned DEFAULT NULL,
  `killPoint` int unsigned DEFAULT NULL,
  `infamia` int unsigned DEFAULT NULL,
  `skillPoint` int unsigned DEFAULT NULL,
  `experience` bigint unsigned NOT NULL,
  `level` tinyint unsigned NOT NULL,
  `guildIndex` int unsigned DEFAULT NULL,
  `gold` int unsigned DEFAULT NULL,
  `positionX` int unsigned NOT NULL,
  `positionY` int unsigned NOT NULL,
  `creationTime` varchar(50) NOT NULL,
  `deleteTime` varchar(50) DEFAULT NULL,
  `loginTime` int unsigned DEFAULT NULL,
  `activeTitle` int unsigned DEFAULT '0',
  `activeAction` int unsigned DEFAULT NULL,
  `teleportPositions` varchar(64) CHARACTER SET latin1 COLLATE latin1_swedish_ci DEFAULT NULL,
  `pranEvolutionCount` int unsigned DEFAULT NULL,
  `savedPositionX` int unsigned DEFAULT NULL,
  `savedPositionY` int unsigned DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=59 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `characters`
--

LOCK TABLES `characters` WRITE;
/*!40000 ALTER TABLE `characters` DISABLE KEYS */;
INSERT INTO `characters` VALUES (56,1,'testr',0,'0123',0,0,40,NULL,'1',0,51,0,7,10,15,9,9,0,7,119,119,0,NULL,NULL,NULL,NULL,NULL,NULL,0,0,NULL,NULL,3450,690,'2025-02-25 02:47:31.496971',NULL,NULL,0,NULL,NULL,NULL,NULL,NULL),(57,1,'Gyyysd',1,'0000',0,0,40,NULL,'1',0,11,0,14,10,6,14,0,0,7,119,119,0,NULL,NULL,NULL,NULL,NULL,NULL,0,0,NULL,NULL,3450,690,'2025-02-25 02:47:50.623880',NULL,NULL,0,NULL,NULL,NULL,NULL,NULL),(58,1,'hgvjgfkj',2,NULL,0,0,40,NULL,'1',0,1,0,15,9,5,16,0,0,7,119,119,0,NULL,NULL,NULL,NULL,NULL,NULL,0,0,NULL,NULL,3450,690,'2025-02-25 02:48:04.317607',NULL,NULL,0,NULL,NULL,NULL,NULL,NULL);
/*!40000 ALTER TABLE `characters` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `itens`
--

DROP TABLE IF EXISTS `itens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `itens` (
  `ownerId` int unsigned NOT NULL,
  `itemId` int unsigned NOT NULL DEFAULT '0',
  `slot` int unsigned NOT NULL,
  `slotType` int NOT NULL,
  `app` int unsigned DEFAULT NULL,
  `identification` int unsigned DEFAULT NULL,
  `effectIndex1` int unsigned DEFAULT NULL,
  `effectValue1` int unsigned DEFAULT NULL,
  `effectIndex2` int unsigned DEFAULT NULL,
  `effectValue2` int unsigned DEFAULT NULL,
  `effectIndex3` int unsigned DEFAULT NULL,
  `effectValue3` int unsigned DEFAULT NULL,
  `minimalItemValue` int unsigned DEFAULT NULL,
  `maxItemValue` int unsigned DEFAULT NULL,
  `refine` int unsigned DEFAULT '1',
  `time` int unsigned DEFAULT NULL,
  PRIMARY KEY (`ownerId`,`slot`,`slotType`,`itemId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `itens`
--

LOCK TABLES `itens` WRITE;
/*!40000 ALTER TABLE `itens` DISABLE KEYS */;
INSERT INTO `itens` VALUES (56,60,0,0,0,0,0,0,0,0,0,0,0,0,0,0),(56,7721,0,1,0,0,0,0,0,0,0,0,0,0,0,0),(57,20,0,0,0,0,0,0,0,0,0,0,0,0,0,0),(57,7722,0,1,0,0,0,0,0,0,0,0,0,0,0,0),(58,10,0,0,0,0,0,0,0,0,0,0,0,0,0,0),(58,7712,0,1,0,0,0,0,0,0,0,0,0,0,0,0);
/*!40000 ALTER TABLE `itens` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `nations`
--

DROP TABLE IF EXISTS `nations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `nations` (
  `nationId` int unsigned NOT NULL AUTO_INCREMENT,
  `nationName` varchar(32) NOT NULL,
  `channelId` int unsigned NOT NULL,
  `nationRank` int unsigned NOT NULL,
  `guildMarshalId` int unsigned NOT NULL,
  `guildTacticianId` int unsigned NOT NULL,
  `guildJudgeId` int unsigned NOT NULL,
  `guildTreasurerId` int unsigned NOT NULL,
  `citizenTax` int unsigned NOT NULL,
  `visitorTax` int unsigned NOT NULL,
  `settlement` int unsigned NOT NULL,
  `nationAlly` int unsigned NOT NULL,
  `marechalAlly` varchar(32) DEFAULT NULL,
  `allyDate` int unsigned NOT NULL,
  `nationGold` bigint unsigned NOT NULL,
  `cercoGuildidAttackA1` int unsigned NOT NULL,
  `cercoGuildidAttackA2` int unsigned NOT NULL,
  `cercoGuildidAttack_A3` int unsigned NOT NULL,
  `cercoGuildAttackIdkA4` int unsigned NOT NULL,
  `cercoGuildAttackIdkB1` int unsigned NOT NULL,
  `cercoGuildAttackIdkB2` int unsigned NOT NULL,
  `cercoGuildAttackIdkB3` int unsigned NOT NULL,
  `cercoGuildAttackIdkB4` int unsigned NOT NULL,
  `cercoGuildAttackIdkC1` int unsigned NOT NULL,
  `cercoGuildAttackIdkC2` int unsigned NOT NULL,
  `cercoGuildAttackIdkC3` int unsigned NOT NULL,
  `cercoGuildAttackIdkC4` int unsigned NOT NULL,
  `cercoGuildAttackIdkD1` int unsigned NOT NULL,
  `cercoGuildAttackIdkD2` int unsigned NOT NULL,
  `cercoGuildAttackIdkD3` int unsigned NOT NULL,
  `cercoGuildAttackIdkD4` int unsigned NOT NULL,
  PRIMARY KEY (`nationId`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `nations`
--

LOCK TABLES `nations` WRITE;
/*!40000 ALTER TABLE `nations` DISABLE KEYS */;
/*!40000 ALTER TABLE `nations` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `server`
--

DROP TABLE IF EXISTS `server`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `server` (
  `nationId` int NOT NULL AUTO_INCREMENT,
  `nationName` varchar(64) NOT NULL,
  `nationPlayerOn` int NOT NULL,
  PRIMARY KEY (`nationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `server`
--

LOCK TABLES `server` WRITE;
/*!40000 ALTER TABLE `server` DISABLE KEYS */;
/*!40000 ALTER TABLE `server` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-02-25  2:22:54
