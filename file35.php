<?php

// Conduction/CommonGroundBundle/Service/KadasterService.php

/*
 * This file is part of the Conduction Common Ground Bundle
 *
 * (c) Conduction <info@conduction.nl>
 *
 * For the full copyright and license information, please view the LICENSE
 * file that was distributed with this source code.
 */

namespace App\Service;

//use Doctrine\ORM\EntityManager;
use App\Entity\Address;
use App\Entity\Company;
use DateTime;
use Doctrine\ORM\EntityManagerInterface;
use GuzzleHttp\Client;
use Ramsey\Uuid\Uuid;
use Symfony\Component\Cache\Adapter\AdapterInterface as CacheInterface;
use Symfony\Component\DependencyInjection\ParameterBag\ParameterBagInterface;

class KvkService
{
    private $config;
    private $params;
    private $client;
    private $commonGroundService;
    private $manager;
    /**
     * @var CacheInterface
     */
    private $cache;

    public function __construct(ParameterBagInterface $params, CacheInterface $cache, EntityManagerInterface $manager)
    {
        $this->params = $params;
        $this->cache = $cache;
        $this->manager = $manager;

        $this->client = new Client([
            // Base URI is used with relative requests
            'base_uri' => $this->params->get('common_ground.components')['kvk']['location'],
            // You can set any number of default request options.
            'timeout'  => 4000.0,
            // This api key needs to go into params
            'headers' => [],
        ]);
    }

    public function getCompany(string $branchNumber)
    {
        $item = $this->cache->getItem('company_'.md5($branchNumber));
        if ($item->isHit()) {
            return $item->get();
        }
        $query = ['branchNumber'=>$branchNumber, 'branch'=>'false', 'mainBranch'=>'true', 'user_key'=>$this->params->get('common_ground.components')['kvk']['apikey']];
        $response = $this->client->get('companies', ['query'=>$query])->getBody();
//        var_dump($response);

        $response = json_decode($response, true);
        $response = $response['data']['items'][0];

        $item->set($response);
        $item->expiresAt(new DateTime('tomorrow 4:59'));
        $this->cache->save($item);

        return $item->get();
    }

    public function getCompanies(array $query)
    {
        $item = $this->cache->getItem('companies_'.md5(implode('', $query)));
        if ($item->isHit()) {
            return $item->get();
        }
        $query['user_key'] = "ccYtzndWPSQvUIwu3qXxCuhOlaDxLeMWuPuMjOhCTRK283H5j";
        $response = $this->client->get('companies', ['query'=>$query])->getBody();
//        var_dump($response);

        $response = json_decode($response, true);
        $response = $response['data']['items'];

        $item->set($response);
        $item->expiresAt(new DateTime('tomorrow 4:59'));
        $this->cache->save($item);

        return $item->get();
    }

    public function getObject($branch): Company
    {
//        var_dump($branch);
        $company = new Company();
        $company->setBranchNumber($branch['branchNumber']);
        $company->setKvkNumber($branch['kvkNumber']);
        if (key_exists('rsin', $branch)) {
            $company->setRsin($branch['rsin']);
        }

        $company->setHasEntryInBusinessRegister($branch['hasEntryInBusinessRegister']);
        $company->setHasNonMailingIndication($branch['hasNonMailingIndication']);

        $company->setIsLegalPerson($branch['isLegalPerson']);
        $company->setIsBranch($branch['isBranch']);
        $company->setIsMainBranch($branch['isMainBranch']);

        foreach ($branch['addresses'] as $rawAddress) {
            $address = new Address();
            $address->setType($rawAddress['type']);
            $address->setStreet($rawAddress['street']);
            $address->setHouseNumber($rawAddress['houseNumber']);
            $address->setHouseNumberAddition($rawAddress['houseNumberAddition']);
            $address->setPostalCode($rawAddress['postalCode']);
            $address->setCity($rawAddress['city']);
            $address->setCountry($rawAddress['country']);

            $this->manager->persist($address);

            $address->setId(Uuid::uuid4());
            $this->manager->persist($address);

            $company->addAddress($address);
        }
        if (key_exists('tradeNames', $branch)) {
            $company->setTradeNames($branch['tradeNames']);
            if (key_exists('businessName', $branch['tradeNames'])) {
                $company->setName($branch['tradeNames']['businessName']);
            } elseif (!is_array($branch['tradeNames'][0])) {
                $company->setName($branch['tradeNames'][0]);
            } else {
                $company->setName($branch['branchNumber']);
            }
        } else {
            $company->setName($branch['branchNumber']);
        }

        // Let see what we got here in terms of object

        $this->manager->persist($company);
        $company->setId($branch['branchNumber']);
        $this->manager->persist($company);

        return $company;
    }

    public function getCompaniesOnSearchParameters($query): array
    {
        // Lets start with th getting of nummer aanduidingen
//        var_dump($query);
//        die;
        $companies = $this->getCompanies($query);

        // Lets setup an responce
        $results = [];
        // Then we need to enrich that
        foreach ($companies as $nummeraanduiding) {
            $results[] = $this->getObject($nummeraanduiding);
        }

        return $results;
    }

    public function getCompanyOnBranchNumber($branchNumber): Company
    {
        $company = $this->getCompany($branchNumber);

        return $this->getObject($company);
    }
}
